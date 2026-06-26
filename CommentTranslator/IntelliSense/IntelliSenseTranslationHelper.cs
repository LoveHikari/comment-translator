using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Framework;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;

namespace CommentTranslator.IntelliSense
{
    /// <summary>
    /// IntelliSense 相关路径共用的翻译与展示拼装逻辑（补全说明、签名帮助文档等）。
    /// </summary>
    internal static class IntelliSenseTranslationHelper
    {
        private static readonly ConcurrentDictionary<string, string> s_cache =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        private const int MaxCacheEntries = 400;

        /// <summary>
        /// 将原始说明与译文在 UI 中纵向堆叠，风格与补全说明保持一致。
        /// </summary>
        public static object ComposeStackedWithTranslation(object originalDescription, string translatedPlainText)
        {
            var translationBlock = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "[译] "),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, translatedPlainText)));

            if (originalDescription == null)
            {
                return translationBlock;
            }

            return new ContainerElement(
                ContainerElementStyle.Stacked,
                originalDescription,
                translationBlock);
        }

        /// <summary>
        /// 仅返回已缓存译文，不发起网络请求（供 UI 线程同步 getter 使用）。
        /// </summary>
        public static string TryGetCachedTranslation(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return null;
            }

            s_cache.TryGetValue(plainText, out string cached);
            return cached;
        }

        /// <summary>
        /// 在后台预取译文写入缓存，不阻塞调用线程。
        /// </summary>
        public static void PrefetchTranslate(string plainText)
        {
            if (string.IsNullOrEmpty(plainText) || s_cache.ContainsKey(plainText))
            {
                return;
            }

            string key = plainText;
            PretreatPlainText(ref key);
            if (string.IsNullOrEmpty(key) || s_cache.ContainsKey(key))
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await GetOrTranslateAsync(key, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // 预取失败不影响签名帮助展示。
                }
            });
        }

        /// <summary>
        /// 异步翻译并写入缓存（供补全 <see cref="System.Threading.Tasks.Task"/> 路径使用）。
        /// </summary>
        public static async Task<string> GetOrTranslateAsync(string plainText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return null;
            }

            if (s_cache.TryGetValue(plainText, out string cached))
            {
                return cached;
            }

            ApiResponse response = await CommentTranslatorPackage.TranslateClient.TranslateAsync(plainText)
                .ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            string target = response.Success ? response.TargetText : null;
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            TryAddToCache(plainText, target);
            return target;
        }

        /// <summary>
        /// 在属性 getter 等同步上下文中阻塞调用翻译；使用 <see cref="ThreadHelper.JoinableTaskFactory"/>
        /// 以避免在 VS UI 线程上与异步衔接不当导致死锁。
        /// </summary>
        public static string GetOrTranslateBlocking(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return null;
            }

            if (s_cache.TryGetValue(plainText, out string cached))
            {
                return cached;
            }

            try
            {
                ApiResponse response = ThreadHelper.JoinableTaskFactory.Run(async () =>
                    await CommentTranslatorPackage.TranslateClient.TranslateAsync(plainText));
                string target = response.Success ? response.TargetText : null;
                if (!string.IsNullOrEmpty(target))
                {
                    TryAddToCache(plainText, target);
                }

                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 供 Quick Info 使用的 <see cref="ClassifiedTextRun"/> 列表，前缀与补全 tooltip 一致为 [译]。
        /// </summary>
        public static IEnumerable<ClassifiedTextRun> ComposeClassifiedRunsWithTranslation(string translatedPlainText)
        {
            if (string.IsNullOrEmpty(translatedPlainText))
            {
                return null;
            }

            return new[]
            {
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "[译] "),
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, translatedPlainText)
            };
        }

        /// <summary>
        /// 将原始纯文本文档与 [译] 译文拼接（供签名帮助等仅支持 string 的 API 使用）。
        /// </summary>
        public static string ComposePlainTextWithTranslation(string original, string translatedPlainText)
        {
            if (string.IsNullOrEmpty(translatedPlainText))
            {
                return original;
            }

            if (string.IsNullOrEmpty(original))
            {
                return "[译] " + translatedPlainText;
            }

            return original + Environment.NewLine + Environment.NewLine + "[译] " + translatedPlainText;
        }

        /// <summary>
        /// 异步翻译并格式化为带 [译] 的纯文本说明。
        /// </summary>
        public static async Task<string> BuildTranslatedPlainDocumentationAsync(
            string original,
            CancellationToken cancellationToken = default)
        {
            if (original == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(original))
            {
                return original;
            }

            string plain = original;
            PretreatPlainText(ref plain);
            if (string.IsNullOrEmpty(plain))
            {
                return original;
            }

            string translated = await GetOrTranslateAsync(plain, cancellationToken).ConfigureAwait(false);
            return ComposePlainTextWithTranslation(original, translated);
        }

        /// <summary>
        /// 与 QuickInfo 一致：去掉前导标点，减少无意义翻译请求。
        /// </summary>
        public static void PretreatPlainText(ref string str)
        {
            var count = 0;
            var temp = str.TrimStart();
            foreach (var ch in temp)
            {
                if (char.IsPunctuation(ch))
                {
                    count++;
                }
                else
                {
                    break;
                }
            }

            str = temp.Substring(count).Trim();
        }

        private static void TryAddToCache(string plainText, string target)
        {
            if (s_cache.Count < MaxCacheEntries)
            {
                s_cache.TryAdd(plainText, target);
            }
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace CommentTranslator.QuickInfo
{
    /// <summary>
    /// 同一次 Quick Info 会话中，多个 subject buffer 会各自调用翻译源；此守卫保证只贡献一条 [译]。
    /// </summary>
    internal static class QuickInfoSessionTranslationGuard
    {
        private static readonly ConditionalWeakTable<IAsyncQuickInfoSession, ClaimState> s_sessions =
            new ConditionalWeakTable<IAsyncQuickInfoSession, ClaimState>();

        private sealed class ClaimState
        {
            internal readonly object Gate = new object();
            internal ITextBuffer Buffer;
            internal int Priority = 100;
            internal bool Contributed;
        }

        /// <summary>
        /// 在发起翻译前同步声明本 buffer 为候选；优先级更高的 buffer 会取代低优先级声明。
        /// </summary>
        internal static bool TryClaimSourceBuffer(IAsyncQuickInfoSession session, ITextBuffer buffer)
        {
            if (session == null || buffer == null)
            {
                return false;
            }

            ClaimState state = s_sessions.GetValue(session, _ => new ClaimState());
            int priority = GetBufferPriority(buffer.ContentType?.TypeName);

            lock (state.Gate)
            {
                if (state.Contributed)
                {
                    return false;
                }

                if (state.Buffer == null)
                {
                    state.Buffer = buffer;
                    state.Priority = priority;
                    return true;
                }

                if (ReferenceEquals(state.Buffer, buffer))
                {
                    return true;
                }

                if (priority < state.Priority)
                {
                    state.Buffer = buffer;
                    state.Priority = priority;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 异步翻译完成后确认仍由本 buffer 输出，并标记会话已贡献译文（防止重复 QuickInfoItem）。
        /// </summary>
        internal static bool TryCompleteContribution(IAsyncQuickInfoSession session, ITextBuffer buffer)
        {
            if (session == null || buffer == null)
            {
                return false;
            }

            if (!s_sessions.TryGetValue(session, out ClaimState state))
            {
                return false;
            }

            lock (state.Gate)
            {
                if (state.Contributed)
                {
                    return false;
                }

                if (!ReferenceEquals(state.Buffer, buffer))
                {
                    return false;
                }

                state.Contributed = true;
                return true;
            }
        }

        internal static int GetBufferPriority(string contentTypeName)
        {
            if (string.IsNullOrEmpty(contentTypeName))
            {
                return 100;
            }

            if (string.Equals(contentTypeName, "CSharp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentTypeName, "Basic", StringComparison.OrdinalIgnoreCase)
                || contentTypeName.StartsWith("CSharp", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(contentTypeName, "code", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(contentTypeName, "text", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(contentTypeName, "projection", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (string.Equals(contentTypeName, "intellisense", StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }

            return 50;
        }
    }
}

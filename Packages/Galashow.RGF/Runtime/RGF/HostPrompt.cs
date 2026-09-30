using System;
using System.Collections.Generic;

namespace Galashow.RGF
{
    /// <summary>
    /// 호스트 선택 팝업 선택지 (React 팝업에 표시)
    /// </summary>
    public sealed class HostPromptOption
    {
        /// <summary>
        /// 호스트가 고르면 HostInput value로 돌아오는 값
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 화면 번호 (시청자 채팅 번호와 같게)
        /// </summary>
        public int Number { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }
    }

    /// <summary>
    /// 호스트에게 선택을 요청하는 팝업. 게임 공통이며 React가 띄우고, 고른 값은 RGFManager_HostInput(command, value=옵션 Id)로 돌아온다.
    /// </summary>
    public sealed class HostPrompt
    {
        public string PromptId { get; set; }

        /// <summary>
        /// HostInput command (예: "choice")
        /// </summary>
        public string Command { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// 선택 버튼 문구 (예: "지키기")
        /// </summary>
        public string ActionLabel { get; set; }

        /// <summary>
        /// 팝업 안내 (예: "시청자는 채팅으로 1 또는 2")
        /// </summary>
        public string Hint { get; set; }

        public List<HostPromptOption> Options { get; set; } = new List<HostPromptOption>();
    }

    /// <summary>
    /// 플러그인이 연 호스트 팝업을 브리지 어댑터로 전달하는 통로 (RGF가 Bridge를 몰라도 되게)
    /// </summary>
    public static class HostPromptBus
    {
        public static event Action<HostPrompt> Opened;
        public static event Action<string> Closed;

        /// <summary>
        /// 현재 열린 팝업 (없으면 null)
        /// </summary>
        public static HostPrompt Current { get; private set; }

        internal static void Open(HostPrompt prompt)
        {
            Current = prompt;
            Opened?.Invoke(prompt);
        }

        internal static void Close(string promptId)
        {
            if (Current == null || Current.PromptId != promptId)
            {
                return;
            }
            Current = null;
            Closed?.Invoke(promptId);
        }
    }
}

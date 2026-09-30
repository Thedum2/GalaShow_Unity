namespace Galashow.RGF
{
    /// <summary>
    /// 참가자 입력 한 건 (채팅 메시지)
    /// </summary>
    public readonly struct PlayerInput
    {
        /// <summary>
        /// 참가자 ID (GameState.Players 키)
        /// </summary>
        public string PlayerId { get; }

        /// <summary>
        /// 원문 메시지
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 입력 시각 (React가 보낸 epoch ms)
        /// </summary>
        public long Timestamp { get; }

        public PlayerInput(string playerId, string message, long timestamp)
        {
            PlayerId = playerId;
            Message = message;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// 참가자 입력을 받는 플러그인 (선택 구현)
    /// RGFManager.SubmitInput이 현재 실행 중인 플러그인에 전달한다.
    /// </summary>
    public interface IPlayerInputReceiver
    {
        void ReceiveInput(PlayerInput input);
    }

    /// <summary>
    /// 호스트(스트리머) 입력 한 건. 방송 화면이 아니라 React 조작 화면에서 온다.
    /// </summary>
    public readonly struct HostInput
    {
        /// <summary>
        /// 게임별 명령 (예: "choice")
        /// </summary>
        public string Command { get; }

        public string Value { get; }

        public HostInput(string command, string value)
        {
            Command = command;
            Value = value;
        }
    }

    /// <summary>
    /// 호스트 입력을 받는 플러그인 (선택 구현)
    /// </summary>
    public interface IHostInputReceiver
    {
        /// <returns>받아들였으면 true</returns>
        bool ReceiveHostInput(HostInput input);
    }

    /// <summary>
    /// 라운드 강제 중단을 처리하는 플러그인 (선택 구현)
    /// </summary>
    public interface IRoundAbortHandler
    {
        void OnRoundAborted(GameState state);
    }
}

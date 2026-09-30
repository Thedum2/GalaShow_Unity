using Newtonsoft.Json.Linq;

namespace Galashow.RGF
{
    /// <summary>
    /// StartRound의 gameData(JSON)를 플러그인 전용 모델로 변환하는 선택 인터페이스
    /// 구현한 플러그인은 라운드 시작 전 변환·검증을 거치고, 결과가 GameState.GameData에 들어간다.
    /// 구현하지 않은 플러그인은 원본 JToken을 그대로 받는다.
    /// </summary>
    public interface IGameDataParser
    {
        /// <summary>
        /// gameData를 플러그인 모델로 변환. 형식이 잘못되면 예외를 던져 라운드 시작을 거부한다.
        /// </summary>
        /// <param name="data">StartRound gameData (object 또는 array)</param>
        object ParseGameData(JToken data);
    }
}

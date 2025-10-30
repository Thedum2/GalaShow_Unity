using UnityEngine;
using UnityEngine.UI;
using Galashow.Bridge;
using Galashow.Bridge.Model;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using Galashow.Core; 

public class Tester : MonoBehaviour
{
    [Header("Buttons")]
    public Button initializeBtn;
    public Button selectRoundBtn;
    public Button selectEventBtn;
    public Button preStartBtn;
    public Button selectStartBtn;
    public Button postStartBtn;
    public Button endRoundBtn;
    public Button endGameBtn;

    [Header("Output")]
    public Text logOutput;

    // 싱글톤들이 준비될 시간을 약간 기다린 후 실행
    void Start() => Invoke(nameof(LateStart), 0.2f);

    void LateStart()
    {
        if (BridgeManager.Instance == null)
        {
            GLog.Debug("BridgeManager not found!");
            return;
        }

        // 버튼 리스너 연결
        initializeBtn.onClick.AddListener(Test_Initialize);
        selectRoundBtn.onClick.AddListener(Test_SelectRound);
        selectEventBtn.onClick.AddListener(Test_SelectEvent);
        preStartBtn.onClick.AddListener(Test_PreStart);
        selectStartBtn.onClick.AddListener(Test_SelectStart);
        postStartBtn.onClick.AddListener(Test_PostStart);
        endRoundBtn.onClick.AddListener(Test_EndRound);
        endGameBtn.onClick.AddListener(Test_EndGame);

        // Unity -> React 메시지(ACK 포함)를 수신하여 로그에 출력
        BridgeManager.OnMessageSent += (msg) =>
        {
            // 성공/실패에 따라 색상 변경
            string color = msg.ok ? "green" : "red";
            string log = $"<color={color}>[U2R] {msg.type} @ {msg.route}</color>\nData: {JsonConvert.SerializeObject(msg.data)}\n";
            GLog.Debug(log.Replace("\n", "\n")); // 콘솔에서는 줄바꿈이 보이도록
            if (logOutput != null) logOutput.text += log;
        };
    }

    /// <summary>
    /// React에서 메시지를 보내는 것을 흉내 내는 헬퍼 메서드
    /// </summary>
    private void SimulateReactMessage(string route, string type, object data, string id = null)
    {
        var message = new Message
        {
            ok = true,
            type = type,
            route = route,
            id = string.IsNullOrEmpty(id) ? $"r2u_{Guid.NewGuid()}" : id,
            data = data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        };
    
        string jsonMessage = JsonConvert.SerializeObject(message);
        BridgeManager.Instance.ReceiveMessage(jsonMessage);
    }

    // 각 버튼에 연결될 테스트 메서드들 (JSON 시뮬레이션 방식)
    void Test_Initialize()
    {
        var reqData = new Request.R2U.Initialize { PlayerName = "Tester", SessionId = "test-session" };
        SimulateReactMessage("GameManager_Initialize", "REQ", reqData, "init-req-1");
    }

    void Test_SelectRound()
    {
        GLog.Debug("--- Test: SelectRound (via JSON) ---");
        var ntyData = new Notify.R2U.Selected
        {
            Index = 1,
            Title = "테스트 라운드: 당신의 선택은?",
            Choices = new List<Notify.R2U.Selected.Choice>
            {
                new Notify.R2U.Selected.Choice { ChoiceId = 0, Text = "A 선택지" },
                new Notify.R2U.Selected.Choice { ChoiceId = 1, Text = "B 선택지" }
            },
            User = new List<Notify.R2U.Selected.UserData>()
        };
        SimulateReactMessage("SimulationManager_Selected", "NTY", ntyData);
    }
    void Test_SelectEvent()
    {
        GLog.Debug("--- Test: Test_SelectEvent (via JSON) ---");
        var ntyData = new Notify.R2U.SelectEvent()
        {
            UserIndex = 100,
            Select = 3
        };
        SimulateReactMessage("SimulationManager_SelectEvent", "NTY", ntyData);
    }

    void Test_PreStart()
    {
        GLog.Debug("--- Test: PreStart (via JSON) ---");
        SimulateReactMessage("SimulationManager_PreStarted", "REQ", null, "pre-req-1");
    }
    
    void Test_SelectStart()
    {
        GLog.Debug("--- Test: SelectStart (via JSON) ---");
        SimulateReactMessage("SimulationManager_SelectStarted", "NTY", null);
    }

    void Test_PostStart()
    {
        GLog.Debug("--- Test: PostStart (via JSON) ---");
        var reqData = new Request.R2U.PostStarted { ResultChoiceId = 0 }; // 0번 선택지가 결과라고 가정
        SimulateReactMessage("SimulationManager_PostStarted", "REQ", reqData, "post-req-1");
    }

    void Test_EndRound()
    {
        GLog.Debug("--- Test: EndRound (via JSON) ---");
        SimulateReactMessage("SimulationManager_Ended", "NTY", null);
    }

    void Test_EndGame()
    {
        GLog.Debug("--- Test: EndGame (via JSON) ---");
        SimulateReactMessage("GameManager_Ended", "NTY", null);
    }
}

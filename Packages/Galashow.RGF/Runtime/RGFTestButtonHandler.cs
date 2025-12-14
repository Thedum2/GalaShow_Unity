using UnityEngine;
using UnityEngine.UI;

namespace Galashow.RGF
{
    /// <summary>
    /// RGF 테스트 버튼 핸들러
    /// UI 버튼에서 RGFFlowTestExample의 네이티브 샘플 메시지 전송 기능을 호출
    /// </summary>
    public class RGFTestButtonHandler : MonoBehaviour
    {
        [Header("버튼 참조")]
        [SerializeField] private Button initializeButton;
        [SerializeField] private Button registerPluginButton;
        [SerializeField] private Button startRoundButton;

        [Header("RGF Test Example 참조")]
        [SerializeField] private RGFFlowTestExample rgfTestExample;

        private void Start()
        {
            if (rgfTestExample == null)
            {
                rgfTestExample = FindObjectOfType<RGFFlowTestExample>();
                if (rgfTestExample == null)
                {
                    Debug.LogError("[RGFTestButtonHandler] RGFFlowTestExample not found in scene!");
                    return;
                }
            }

            // 버튼 클릭 이벤트 연결
            if (initializeButton != null)
            {
                initializeButton.onClick.AddListener(OnInitializeButtonClicked);
            }

            if (registerPluginButton != null)
            {
                registerPluginButton.onClick.AddListener(OnRegisterPluginButtonClicked);
            }

            if (startRoundButton != null)
            {
                startRoundButton.onClick.AddListener(OnStartRoundButtonClicked);
            }
        }

        private void OnInitializeButtonClicked()
        {
            rgfTestExample?.SendNativeSample_Initialize();
        }

        private void OnRegisterPluginButtonClicked()
        {
            rgfTestExample?.SendNativeSample_RegisterPlugin();
        }

        private void OnStartRoundButtonClicked()
        {
            rgfTestExample?.SendNativeSample_StartRound();
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (initializeButton != null)
            {
                initializeButton.onClick.RemoveListener(OnInitializeButtonClicked);
            }

            if (registerPluginButton != null)
            {
                registerPluginButton.onClick.RemoveListener(OnRegisterPluginButtonClicked);
            }

            if (startRoundButton != null)
            {
                startRoundButton.onClick.RemoveListener(OnStartRoundButtonClicked);
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Galashow.Bridge;
using Newtonsoft.Json;


public class InterfaceSampleManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField route_inputField;
    [SerializeField] private TMP_InputField datajson_inputField;

    [SerializeField] private Button REQ;
    [SerializeField] private Button ACK;
    [SerializeField] private Button NTY;
    
    [SerializeField] private Button ChangeBorderColor_B;
    [SerializeField] private Button CalculateAdd_B;
    [SerializeField] private Button CalculateMultiply_B;

    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }

    private void InitializeUI()
    {
    
    }

    private void SetupEventListeners()
    {
        REQ.onClick.AddListener(OnREQButtonClicked);
        ACK.onClick.AddListener(OnACKButtonClicked);
        NTY.onClick.AddListener(OnNTYButtonClicked);
        ChangeBorderColor_B.onClick.AddListener(LoadChangeBorderColor_B);
        CalculateAdd_B.onClick.AddListener(LoadCalculateAdd_B);
        CalculateMultiply_B.onClick.AddListener(LoadCalculateMultiply_B);
        
        BridgeManager.OnMessageReceived += OnMessageReceived;
        BridgeManager.OnMessageSent += OnMessageSent;
    }

    private void OnDestroy()
    {
        REQ.onClick.RemoveAllListeners();
        ACK.onClick.RemoveAllListeners();
        NTY.onClick.RemoveAllListeners();
        ChangeBorderColor_B.onClick.RemoveAllListeners();
        CalculateAdd_B.onClick.RemoveAllListeners();
        CalculateMultiply_B.onClick.RemoveAllListeners();
        
        BridgeManager.OnMessageReceived -= OnMessageReceived;
        BridgeManager.OnMessageSent -= OnMessageSent;
    }

    #region Button Event Handlers

    private void OnREQButtonClicked()
    {
        string route = route_inputField.text;
        string dataJson = datajson_inputField.text;
        
        if (string.IsNullOrEmpty(route))
        {
            LogMessage("Error: Route is required for REQ");
            return;
        }
        
        object data = ParseJsonData(dataJson);
        
        LogMessage($"Sending REQ to route: {route}");
        
        BridgeManager.Instance.SendRequest(
            route: route,
            data: data,
            onSuccess: (response) => {
                LogMessage($"REQ Success - ID: {response.id}, Data: {JsonConvert.SerializeObject(response.data)}");
            },
            onError: (error) => {
                LogMessage($"REQ Error: {error}");
            },
            onTimeout: () => {
                LogMessage("REQ Timeout");
            }
        );
        
        route_inputField.text = "";
        datajson_inputField.text = "";
    }

    private void OnACKButtonClicked()
    {
        route_inputField.text = "";
        datajson_inputField.text = "";
    }

    private void OnNTYButtonClicked()
    {
        string route = route_inputField.text;
        string dataJson = datajson_inputField.text;
        
        if (string.IsNullOrEmpty(route))
        {
            LogMessage("Error: Route is required for NTY");
            return;
        }
        
        object data = ParseJsonData(dataJson);
        
        LogMessage($"Sending NTY to route: {route}");
        
        BridgeManager.Instance.SendNotify(route, data);
        route_inputField.text = "";
        datajson_inputField.text = "";
    }

    #endregion

    #region Message Event Handlers

    private void OnMessageReceived(Message message)
    {
        LogMessage($"← Received {message.type}: {message.route} (ID: {message.id})");
        if (message.data != null)
        {
            LogMessage($"  Data: {JsonConvert.SerializeObject(message.data)}");
        }
        if (!string.IsNullOrEmpty(message.error))
        {
            LogMessage($"  Error: {message.error}");
        }
    }

    private void OnMessageSent(Message message)
    {
        LogMessage($"→ Sent {message.type}: {message.route} (ID: {message.id})");
        if (message.data != null)
        {
            LogMessage($"  Data: {JsonConvert.SerializeObject(message.data)}");
        }
    }

    #endregion

    #region Utility Methods

    private object ParseJsonData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData) || jsonData.Trim() == "{}")
        {
            return null;
        }
        
        try
        {
            return JsonConvert.DeserializeObject(jsonData);
        }
        catch (JsonException e)
        {
            LogMessage($"JSON Parse Error: {e.Message}");
            return null;
        }
    }

    private void LogMessage(string message)
    {
        Debug.Log($"[InterfaceSampleManager] {message}");
    }

    #endregion

    #region Sample Data Presets

    private void LoadChangeBorderColor_B()
    {
        route_inputField.text = "SampleManager_ChangeSphereColor";
        datajson_inputField.text = "{\"color\":\"#0000FF\"}";
    }

    private void LoadCalculateAdd_B()
    {
    }

    private void LoadCalculateMultiply_B()
    {
    }

    #endregion
}
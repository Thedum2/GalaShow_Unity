using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Galashow.Controller;
using Galashow.Bridge.Model;
using System;
using System.Linq;
using Newtonsoft.Json;

public class InterfaceSampleManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown route_dropdown;
    [SerializeField] private TMP_InputField data_inputField;

    [SerializeField] private Button ChangeBorderColor_B;
    [SerializeField] private Button CalculateMultiply_B;

    private void Awake()
    {
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        if (ChangeBorderColor_B != null) ChangeBorderColor_B.onClick.AddListener(OnChangeBorderColorButtonClicked);
        if (CalculateMultiply_B != null) CalculateMultiply_B.onClick.AddListener(OnCalculateMultiplyButtonClicked);
    }

    #region 버튼 이벤트 핸들러
    
    private void OnChangeBorderColorButtonClicked()
    {
        SampleManager.Instance.SendChangeBorderColor(data_inputField.text);
    }
    private void OnCalculateMultiplyButtonClicked()
    {
        try
        {
            int[] numbers = data_inputField.text.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            int a = numbers[0];
            int b = numbers[1];
            SampleManager.Instance.SendCalculateMultiply(a, b, 
                (r) =>
                {
                    data_inputField.text = r.ToString();
                },
                () =>
                {
                    
                }
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"CalculateMultiply 요청 오류: {ex.Message}");
        }
    }
    #endregion
}
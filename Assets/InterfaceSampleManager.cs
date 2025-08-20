using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Galashow.Controller;
using System;
using System.Linq;

public class InterfaceSampleManager : MonoBehaviour
{
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
        SampleManager.Instance.SendChangeBorderColor("#FF0000");
    }
    private void OnCalculateMultiplyButtonClicked()
    {
        try
        {
            SampleManager.Instance.SendCalculateMultiply(5,7, 
                (r) =>
                {
                    Debug.LogError($"아싸성공");
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
using TMPro;
using UnityEngine;
using MCRGame.UI;
using DG.Tweening;

public class YakuObject : MonoBehaviour
{
    [SerializeField] private TMP_Text yakuNameText;
    [SerializeField] private TMP_Text scoreText;


    public void SetYakuInfo(string yakuName, string score)
    {
        yakuNameText.text = yakuName;
        scoreText.text = score;
    }
}
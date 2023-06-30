using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Question : MonoBehaviour
{
    [SerializeField]
    private QuestionnaireManager questionnaireManager;

    [SerializeField]
    private ToggleGroup toggleGroup;

    [SerializeField]
    private int id = 0;
    [SerializeField]
    private string question = "Question";

    [SerializeField]
    private TMP_Text questionTMP;

    [SerializeField]
    private int answer = 0;

    [SerializeField]
    private Toggle[] likertScaleToggles;


    // Start is called before the first frame update
    void Start()
    {
        questionTMP.text = question;

        for (int i=0; i<likertScaleToggles.Length; i++){
            int j = i;
            likertScaleToggles[j].onValueChanged.AddListener(delegate {
                ToggleValueChanged(likertScaleToggles[j],j);
            });
        }
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ToggleValueChanged(Toggle toggle, int i)
    {
         if (toggle.isOn){
            answer = i;
         }
         questionnaireManager.UpdateAnswers();
    }

    public int GetAnswer(){
        return answer;
    }

    public void Reset(){
        toggleGroup.SetAllTogglesOff();
        answer = 0;
    }

    public bool AnswerSelected(){
        return toggleGroup.AnyTogglesOn();
    }

    public void Show(){
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }

    public void Hide(){
        gameObject.transform.localScale = new Vector3(0, 0, 0);
    }
}

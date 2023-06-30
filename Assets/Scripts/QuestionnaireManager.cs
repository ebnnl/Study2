using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class QuestionnaireManager : MonoBehaviour
{
    // Every question in one page like paper version?

    [SerializeField]
    private Question[] questions;

    [SerializeField]
    private StudyManager studyManager;
    [SerializeField]
    private StudyManager trainingStudyManager;

    [SerializeField]
    private int[] answers;

    [SerializeField]
    private Button nextButton;
    [SerializeField]
    private Button previousButton;
    [SerializeField]
    private Button confirmButton;

    private int currentQuestion = 0;

    private bool training = false;

    // Start is called before the first frame update
    void Start()
    {
        answers = new int[questions.Length];

        nextButton.onClick.AddListener(NextQuestion);
        previousButton.onClick.AddListener(PreviousQuestion);
        confirmButton.onClick.AddListener(Confirm);

        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        // Disable next if no answer selected
        if(questions[currentQuestion].AnswerSelected()){
            nextButton.interactable = true;
            confirmButton.interactable = true;
        }
        else{
            nextButton.interactable = false;
            confirmButton.interactable = false;
        }
    }

    public void UpdateAnswers(){
        for (int i=0; i<questions.Length; i++){
            answers[i] = questions[i].GetAnswer();
        }
    }

    void Initialise(){
        currentQuestion = 0;

        // Reset and display first question
        for (int i=0; i<questions.Length; i++){
            questions[i].Reset();
            questions[i].Hide();
            answers[i] = 0;
        }
        questions[0].Show();

        // Disable previous button
        previousButton.interactable = false;
        // Hide confirm button
        confirmButton.gameObject.SetActive(false);
        // Show next button
        nextButton.gameObject.SetActive(true);
    }

    void NextQuestion(){
        // Hide previous question
        questions[currentQuestion].Hide();
        currentQuestion++;
        // Show new question
        questions[currentQuestion].Show();
        if(currentQuestion+1>=questions.Length){
            // Hide next
            nextButton.gameObject.SetActive(false);
            // Show confirm button
            confirmButton.gameObject.SetActive(true);
        }
        if(currentQuestion>0){
            // Enable previous
            previousButton.interactable = true;
        }
    }

    void PreviousQuestion(){
        if(currentQuestion>0){
            // Hide current question
            questions[currentQuestion].Hide();
            currentQuestion--;
            // Show current question
            questions[currentQuestion].Show();
        }
        if (currentQuestion==0){
            // Disable previous
            previousButton.interactable = false;
        }
        if(currentQuestion+1<questions.Length){
            // Hide confirm
            confirmButton.gameObject.SetActive(false);
            // Show next button
            nextButton.gameObject.SetActive(true);
        }
    }

    void Confirm(){
        if(training){
            //trainingStudyManager.SetAnswers(answers);
        }
        else{
            //studyManager.SetAnswers(answers);
        }
        
        
        // Reset questionnaire
        Initialise();

        // Hide questionnaire
        gameObject.SetActive(false);

        // Move on to next item
        if(training){
            trainingStudyManager.EndQuestionnaire();
        }
        else{
            studyManager.EndQuestionnaire();
        }
    }

    public void setTraining(bool b){
        training = b;
    }

    
}

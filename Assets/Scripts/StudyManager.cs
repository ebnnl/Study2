using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text; 
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public class StudyManager : MonoBehaviour, Photon.Pun.IPunObservable
{
    [SerializeField]
    private bool training = false;

    // Questionnaire
    //[SerializeField]
    //private GameObject questionnaireGO;

    //[SerializeField]
    //private int nbQuestions;

    [SerializeField]
    private GameObject[] objects;

    //################# DATA ########################
    [Header("Participant Data")]
    private int participantID = 0;
    
    [SerializeField]
    private TextAsset sets;
    [SerializeField]
    private TextAsset sources;
    [SerializeField]
    private TextAsset sequence;
    [SerializeField]
    private TextAsset realGameSequenceCsv;
    [SerializeField]
    private TextAsset virtualGameSequenceCsv;
    
    private TextWriter outputData;

    private int[] setSource = new int[3]; //setSource[i] is the source of set i for current participant
    private int[] objectSet = new int[30]; //objectSet[i] is the set of object i (same for every participant)
    private int[] objectSource = new int[30]; //objectSource[i] is the source of object i for current participant

    private int[] objectSequence = new int[20]; //sequence of objects that current participant will see
    private int[] sourceSequence = new int[20]; //sequence of sources that participant will be exposed to
    private int[] gamesSequence = new int[20]; //sequence of games the participant will play
    
    
    //################# STATE MACHINE ########################
    // Input
    //public InputActionProperty buttonAction;

    //First ask for participant number
    // Press button to start the study
    // Tells which game to play, start timer
    // When timer ends, "bip", ask to close eyes
    // Display object on table
    // "bip", ask to open eyes, look at the table
    // "bip", ask to fill presence questionnaire
    // when done, tell if stay in VR (press button to start new game)
    // or tell to remove headset

    // Sources
    public const int REAL = 1;
    public const int VIRTUAL = 2;
    public const int NEW = 0;

    // States
    public const int WAITING = 3;
    public const int PLAYING = 4;
    public const int EYESCLOSED = 5;
    public const int OBSERVING = 6;
    public const int QUESTIONNAIRE = 7;
    public const int END = 8;
    public const int GOINGTOCROSS = 9;
 
    // Current progress in the sequence

    private int progress = 0;
    private int currentState = WAITING;
    private int previousState = GOINGTOCROSS;
    private int currentSource = REAL;
    private int previousSource = REAL;
    private int currentObject = 0;
    private int currentGame = 0;
    //private int[] currentQuestionnaireAnswers;

    private float gameTimer;
    private float eyesClosedTimer;
    private float observingTimer;

     // Timers
    [Header("Timers")]
    [SerializeField]
    private float gameDuration = 60;
    [SerializeField]
    private float eyesClosedDuration = 5;
    [SerializeField]
    private float observingDuration = 7;

    // Audio
    [Header("Audio instructions")]
    [SerializeField]
    private AudioSource audioSource;
    public AudioClip completeQuestionnaire;
    public AudioClip goBack;
    public AudioClip openEyes;
    public AudioClip playGame1;
    public AudioClip playGame2;
    public AudioClip playGame3;
    public AudioClip putHeadset;
    public AudioClip removeHeadset;
    public AudioClip stopPlaying;
    [SerializeField]
    private float volume = 0.5f;

    public PhotonView photonView;

    [Header("UI")]
    [SerializeField]
    private TMP_Text currentSourceTMP;
    [SerializeField]
    private TMP_Text nextSourceTMP;
    [SerializeField]
    private TMP_Text currentObjectTMP;
    [SerializeField]
    private TMP_Text currentStateTMP;
    [SerializeField]
    private TMP_Text nextObjectTMP;
    [SerializeField]
    private Button hideObjectButton;
    [SerializeField]
    private TMP_Text placeObjectTMP;
    [SerializeField]
    private TMP_Text removeObjectTMP;

    private bool owner = false;
    

    private float previousButtonState;
    
    // Start is called before the first frame update
    void Start()
    {
        InitialiseOutput();
        LoadData();
        HideObjects();
        photonView.RPC("HideObjects", RpcTarget.All);
        PositionObjects();
        photonView.RPC("PositionObjects", RpcTarget.All);

        currentSource = sourceSequence[0];
        currentObject = objectSequence[0];
        currentGame = gamesSequence[0];
        //currentQuestionnaireAnswers = new int[nbQuestions];

        gameTimer = gameDuration;
        eyesClosedTimer = eyesClosedDuration;
        observingTimer = observingDuration;

        // Set initial ownership of the shared object
        if (photonView.IsMine)
        {
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }

        hideObjectButton.onClick.AddListener(delegate { photonView.RPC("HideObjects", RpcTarget.All); });

        hideObjectButton.gameObject.SetActive(false);
        placeObjectTMP.gameObject.SetActive(false);
        removeObjectTMP.gameObject.SetActive(false);

        Wait();

    }

    // Update is called once per frame
    void Update()
    {
        // Press space to request control on the states
        if (Input.GetKeyUp("space") && !owner){
            if (!photonView.IsMine)
            {
                photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            }
            Debug.Log("transfer");
            owner = true;
        }

        ManageStateTriggers();
    }

    void LoadData(){
        // Read csv file with the source of each set for participant p, and store it setSource list 
        string sourcesData = sources.text;
        string[] sourcesLines = sourcesData.Split("\n"[0]);
        string[] lineData = (sourcesLines[participantID].Trim()).Split(";"[0]);
        for (int set=0; set<3; set++){
            string value = lineData[set];
            int source;
            int.TryParse(value, out source);
            setSource[set] = source;
            
        }

        // Read csv file with the list of object for each set and store data in objectSet and objectSource lists
        string setsData = sets.text;
        string[] setsLines = setsData.Split("\n"[0]);
        for(int set=0; set<3; set++){
            lineData = (setsLines[set].Trim()).Split(";"[0]);
            foreach(string value in lineData){
                int objectId;
                int.TryParse(value, out objectId);
                objectSet[objectId] = set;
                objectSource[objectId] = setSource[set];
            }
        }
        
        string sequenceData = sequence.text;
        string[] sequenceLines = sequenceData.Split("\n"[0]);
        string outputLine;
        lineData = (sequenceLines[participantID].Trim()).Split(";"[0]);
        int i=0;
        foreach(string value in lineData){
                int objectId;
                int.TryParse(value, out objectId);
                // Add object to ordered list, with its source (if source is not new)
                Debug.Log(objectId);
                int sourceId = objectSource[objectId];
                if (sourceId!=NEW){
                    objectSequence[i]=objectId;
                    sourceSequence[i]=sourceId;
                    i++;
                } 
                else {
                    outputLine="x;"+objectId.ToString()+";x;NEW;_;_";
                    /*for (int j=0; j<nbQuestions; j++){
                        outputLine+=";x";
                    }*/
                    if(!training){
                        outputData.WriteLine(outputLine);
                    }
                    
                }
            }
        outputData.Flush();

        int[] realGameSequence = new int[10]; //sequence of the real games for current participant
        int[] virtualGameSequence = new int[10]; //sequence of virtual games for current participant

        string realGameSequenceData = realGameSequenceCsv.text;
        string[] realGameSequenceLines = realGameSequenceData.Split("\n"[0]);
        lineData = (realGameSequenceLines[participantID].Trim()).Split(";"[0]);
        i=0;
        foreach(string value in lineData){
                int gameId;
                int.TryParse(value, out gameId);
                realGameSequence[i]=gameId;
                i++;
            }

        string virtualGameSequenceData = virtualGameSequenceCsv.text;
        string[] virtualGameSequenceLines = virtualGameSequenceData.Split("\n"[0]);
        lineData = (virtualGameSequenceLines[participantID].Trim()).Split(";"[0]);
        i=0;
        foreach(string value in lineData){
                int gameId;
                int.TryParse(value, out gameId);
                virtualGameSequence[i]=gameId;
                i++;
            }
        
        int r = 0;
        int v = 0;
        for(int j=0; j<20; j++){
            if (sourceSequence[j]==REAL){
                gamesSequence[j]=realGameSequence[r];
                r++;
            }
            else if (sourceSequence[j]==VIRTUAL){
                gamesSequence[j]=virtualGameSequence[v];
                v++;
            }
        }
    }

    void InitialiseOutput(){
        // Create new output data file
        if(!training){
            outputData = new StreamWriter("data_participant"+participantID.ToString()+".csv", false);
        }
        else{
            outputData = new StreamWriter("training.csv", false);
        }
       

        // First line: participant ID
            
        // Second line: labels
        // Item number, object id, game ID, source, source monitoring result, type of error, q1...
        string line2 = "Item number;Object ID;Game;Source;Source monitoring result;Type of error";
        //for(int i = 0; i<nbQuestions; i++){
            //line2+=";Question "+(i+1).ToString();
        //}

        if (!training){
            outputData.WriteLine("Participant ID;"+participantID.ToString());
            outputData.WriteLine(line2);
        }   
            
        // Lines 3 to 11: list of new object (no data collected for questionnaire)
        // Done in LoadData
        outputData.Flush(); 
    }

 
    [PunRPC]
    void HideObjects(){
        foreach (GameObject gameObject in objects){
            gameObject.SetActive(false);
        }
        //questionnaireGO.SetActive(false);
        hideObjectButton.gameObject.SetActive(false);
    }

    [PunRPC]
    void PositionObjects()
    {
        foreach (GameObject gameObject in objects)
        {
            gameObject.transform.position = new Vector3(0, 0, 0);
        }
        //questionnaireGO.SetActive(false);
        hideObjectButton.gameObject.SetActive(false);
    }

    [PunRPC]
    public void StartPlaying()
    {
        previousState = WAITING;
        currentState = PLAYING;

        currentStateTMP.text = "Playing game ";

        Debug.Log("TRANSITION TO PLAYING");
        Debug.Log("Playing");
        Debug.Log(currentGame);

        previousState = currentState;
    }

    [PunRPC]
    public void CloseEyes()
    {
        // Change state
        previousState = PLAYING;
        currentState = EYESCLOSED;
        // Reset timer
        gameTimer = gameDuration;

        currentStateTMP.text = "Eyes closed ";

        // Voice saying to stop playing
        audioSource.PlayOneShot(stopPlaying, volume);
        //Voice saying close your eyes;
        Debug.Log("Eyes closed");

        if (currentSource == REAL)
        {
            placeObjectTMP.gameObject.SetActive(true);
        }

        previousState = currentState;
    }

    [PunRPC]
    public void Observe()
    {
        // Change state
        previousState = EYESCLOSED;
        currentState = OBSERVING;
        // Reset timer
        eyesClosedTimer = eyesClosedDuration;

        currentStateTMP.text = "Observing ";

        // Show object
        if (currentSource == VIRTUAL)
        {
            objects[currentObject].SetActive(true);
            Debug.Log("DISPLAY OBJECT");
        }
        else
        {
           placeObjectTMP.gameObject.SetActive(false);
        }
        // Voice saying to open eyes
        // Voice saying to look at the table
        audioSource.PlayOneShot(openEyes, volume);

        Debug.Log("Observing");
        Debug.Log("Source:");
        Debug.Log(currentSource);
        Debug.Log("Object:");
        Debug.Log(objects[currentObject].name);

        previousState = currentState;
    }

    [PunRPC]
    public void FillQuestionnaire()
    {
        // Change state
        previousState = OBSERVING;
        currentState = QUESTIONNAIRE;
        // Reset timer
        observingTimer = observingDuration;

        currentStateTMP.text = "Questionnaire ";

        if (currentSource == VIRTUAL)
        {
            hideObjectButton.gameObject.SetActive(true);
        }
        else
        {
            removeObjectTMP.gameObject.SetActive(true);
        }
    
        audioSource.PlayOneShot(completeQuestionnaire, volume);
        Debug.Log("Filling presence questionnaire");

        previousState = currentState;
    }

    [PunRPC]
    public void GoToCross()
    {
        previousState = QUESTIONNAIRE;
        currentState = GOINGTOCROSS;
        currentStateTMP.text = "Going to the cross";

        removeObjectTMP.gameObject.SetActive(false);

        /*questionnaireGO.SetActive(false);*/
        //objects[objectSequence[progress-1]].SetActive(false);
        photonView.RPC("HideObjects", RpcTarget.All);

        Debug.Log("************* Transition");
        /*if (previousSource == currentSource){
            //Voice saying go back to the cross
            audioSource.PlayOneShot(goBack, volume);
        }*/
        if (currentSource == REAL)
        {
            //Go back to the cross
            audioSource.PlayOneShot(goBack, volume);
        }
        else if (currentSource == VIRTUAL)
        {
            //Put headset and go back to the cross 
            audioSource.PlayOneShot(putHeadset, volume);
        }

        previousState = currentState;
    }
    
    [PunRPC]
    public void Wait()
    {
        previousState = GOINGTOCROSS;
        currentState = WAITING;

        photonView.RPC("HideObjects", RpcTarget.All);

        // Display current and next state
        currentObjectTMP.text = objects[currentObject].name;
        currentStateTMP.text = "Waiting to start";
        if (currentSource == VIRTUAL)
        {
            currentSourceTMP.text = "Virtual";
        }
        else
        {
            currentSourceTMP.text = "Real";
        }
        if (progress + 1 < 20)
        {
            int nextSource = sourceSequence[progress + 1];
            int nextObject = objectSequence[progress + 1];
            nextObjectTMP.text = objects[nextObject].name;
            if (nextSource == VIRTUAL)
            {
                nextSourceTMP.text = "Virtual";
            }
            else
            {
                nextSourceTMP.text = "Real";
            }
        }


        Debug.Log("Waiting for participant to start playing, then press button to start timer");
        // Voice saying which game to play
        if (currentGame == 0)
        {
            audioSource.PlayOneShot(playGame1, volume);
        }
        else if (currentGame == 1)
        {
            audioSource.PlayOneShot(playGame2, volume);
        }
        else
        {
            audioSource.PlayOneShot(playGame3, volume);
        }

        previousState = currentState;
    }

    [PunRPC]
    public void EndStudy()
    {
        currentStateTMP.text = "End of study";
        Debug.Log("End of the study");

        previousState = currentState;
    }

    void ManageStateTriggers(){

        if (currentState==WAITING){
            if (Input.GetKeyUp("space")) // Should be researcher's controller button
            {
                photonView.RPC("StartPlaying", RpcTarget.All);
            }
        }

        else if (currentState==PLAYING){
            if (!training && owner){
                // Start timer
                if(gameTimer > 0){
                    gameTimer -= Time.deltaTime;
                    currentStateTMP.text = "Playing game " + gameTimer.ToString();

                    //Debug.Log(gameTimer);
                }   
                // If end of timer 
                else{
                    photonView.RPC("CloseEyes", RpcTarget.All);
                }
            }
            else if (Input.GetKeyUp("space")){
                photonView.RPC("CloseEyes", RpcTarget.All);
            }
            
        }

        else if (currentState==EYESCLOSED){
            //Display the object on the table when researcher press button
            /*if(currentSource==VIRTUAL && Input.GetKeyUp("space")){
                objects[currentObject].SetActive(true);
            }*/

            if(!training && owner){
                // Start timer
                if(eyesClosedTimer > 0){
                    eyesClosedTimer -= Time.deltaTime;
                    currentStateTMP.text = "Eyes closed " + eyesClosedTimer.ToString();
                    //Debug.Log(eyesClosedTimer);
                }
                // If end of timer
                else{
                    photonView.RPC("Observe", RpcTarget.All);
                }
            }
            else if (Input.GetKeyUp("space"))
            {
                photonView.RPC("Observe", RpcTarget.All);
            }
            
        }

        else if (currentState==OBSERVING){
            if (!training && owner){
                // Start timer 
                if (observingTimer > 0){
                    observingTimer -= Time.deltaTime;
                    currentStateTMP.text = "Observing " + observingTimer.ToString();
                    //Debug.Log(observingTimer);
                }
                // If end of timer
                else {
                    photonView.RPC("FillQuestionnaire", RpcTarget.All);
                }
            }
            else if (Input.GetKeyUp("space")){
                photonView.RPC("FillQuestionnaire", RpcTarget.All); 
            }
            
        }

        else if (currentState==QUESTIONNAIRE){
            //Hide the object on the table when researcher press button
            if(currentSource==VIRTUAL && Input.GetKeyUp("a")){
                photonView.RPC("HideObjects", RpcTarget.All);
                //objects[currentObject].SetActive(false);
            }
            
            // When questionnaire filled
            if (Input.GetKeyUp("space"))  
            {
                EndQuestionnaire();
                photonView.RPC("GoToCross", RpcTarget.All);
            }
        }

        else if (currentState==GOINGTOCROSS){
            if (Input.GetKeyUp("space")) // Should be researcher's controller button
            {
                photonView.RPC("Wait", RpcTarget.All);
            }
        }

        else if (currentState==END){
            photonView.RPC("EndStudy", RpcTarget.All);
        }

    }

    [PunRPC]
    public void EndQuestionnaire(){
        //SaveProgressData(); 
        progress++;
        previousState = QUESTIONNAIRE;
        if(progress>=20){
            currentState = END;
        }
        else{
            // Change state
            currentState = GOINGTOCROSS;
        }
        previousSource = sourceSequence[progress-1];
        currentSource = sourceSequence[progress];
        currentObject = objectSequence[progress];
        currentGame = gamesSequence[progress];

        //GoToCross();
    }
    
    void OnApplicationQuit()
    {
        Debug.Log("Application ending after " + Time.time + " seconds");
        outputData.Close();
    }

    public void SetParticipantID(int id){
        participantID = id;
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentState);
            stream.SendNext(progress);
            stream.SendNext(currentSource);
            stream.SendNext(previousSource);
            stream.SendNext(currentObject);
            stream.SendNext(currentGame);
            //stream.SendNext(currentQuestionnaireAnswers);
        }
        else if (stream.IsReading){
            currentState = (int)stream.ReceiveNext();
            progress = (int)stream.ReceiveNext();
            currentSource = (int)stream.ReceiveNext();
            previousSource = (int)stream.ReceiveNext();
            currentObject = (int)stream.ReceiveNext();
            currentGame = (int)stream.ReceiveNext();
            //currentQuestionnaireAnswers = (int[])stream.ReceiveNext();
            //ManageStateTransitions();
        }
            
    }

 
    public void SetVolume(float v){
        volume = v;
    }

}

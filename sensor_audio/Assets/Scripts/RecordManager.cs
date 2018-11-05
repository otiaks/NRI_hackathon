using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
///-----------------------------------------------------------
/// <summary>録音管理</summary>
///-----------------------------------------------------------
public class RecordManager : MonoBehaviour
{
    //============================================================
    //公開変数
    //============================================================

    // #region PUBLIC_PARAMETERS

    //最大の録音時間
    public int maxDuration;

    //音声データ
    public AudioClip audioClip;

    public ParticleSystem particle;

    public GameObject audience;
    private Animator animator;

    //============================================================
    //非公開変数
    //============================================================

    // #region PRIVATE_PARAMETERS

    //録音のサンプリングレート
    private const int sampleRate = 16000;
    //マイクのデバイス名
    private string mic;

    //再生用オーディオソース
    private AudioSource audioSource;

    private AudioSource tempSource;
    ///-----------------------------------------------------------
    /// <summary>初期化処理</summary>
    ///-----------------------------------------------------------

    // API_URL
    private string url = "http://133.130.124.98:3001/external/api/audience";
    // 試合経過時間
    private int elapsed_game_time = 0;

    [System.Serializable]
    public class audio_data{
        public string game_name;
        public int volume;
        public int elapsed_time;
    }

    public audio_data audio_origin = new audio_data();
    bool flag;
    void Start()
    {
        //再生用オーディオソース
        this.audioSource = GetComponent<AudioSource>();
        // キャリーオーバーparticle
        particle = this.GetComponent<ParticleSystem> ();
        animator = audience.GetComponent<Animator> ();

        //マイク存在確認
        if (Microphone.devices.Length == 0)
        {
            Debug.Log("マイクが見つかりません");
            return;
        }
        flag = true;
    }

    void Update()
    {   
        if (flag && Input.GetKeyDown(KeyCode.Space)){
            StartCoroutine(loop());
            Debug.Log("start!");
            flag = false;
        }
    }   

    
    // スタートボタン
    public void StartButton () {
        StartCoroutine(loop());
    }

    private IEnumerator loop() {
        // ループ
        while (true) {
            // 1秒毎にループ
            yield return new WaitForSeconds(1f);
            onTimer();
        }
    }

    private void onTimer() {
        // 1秒毎に呼ばる
        StopRecord();
        elapsed_game_time += 1;

        audio_origin.game_name= "NRI_hack";
        audio_origin.volume = Play();
        Debug.Log(audio_origin.volume);
        audio_origin.elapsed_time = elapsed_game_time;
        string audio_json = JsonUtility.ToJson(audio_origin);

        Debug.Log(audio_json);
        if(audio_origin.volume > 25000) {
            Debug.Log("particle");
            particle.Play();
            animator.SetBool("loud_voice", true);
        }else{
            if(!particle.isEmitting){
                animator.SetBool("loud_voice", false);
            }
        }
        StartCoroutine(Post(url, audio_json));

        StartRecord();
    }



    IEnumerator Post(string url, string myjson)
    {
        byte[] postData = System.Text.Encoding.UTF8.GetBytes (myjson);
        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.Send();
        Debug.Log("Status Code: " + request.responseCode);
    }



    ///-----------------------------------------------------------
    /// <summary>録音開始</summary>
    ///-----------------------------------------------------------
    public void StartRecord()
    {   
        //マイク存在確認
        if (Microphone.devices.Length == 0)
        {
            Debug.Log("マイクが見つかりません");
            return;
        }

        //マイク名
        mic = Microphone.devices[0];
        //録音開始。audioClipに音声データを格納。
        audioClip = Microphone.Start(mic, false, maxDuration, sampleRate);
    }

    ///-----------------------------------------------------------
    /// <summary>録音終了</summary>
    ///-----------------------------------------------------------
    public void StopRecord()
    {
        //マイクの録音位置を取得
        int position = Microphone.GetPosition(mic);
        //マイクの録音を強制的に終了
        Microphone.End(mic);
        Debug.Log("stop!");

        //シーク位置を検査
        if (position > 0)
        {
            //再生時間を確認すると、停止した時間に関わらず、maxDurationの値になっている。これは無音を含んでいる？

            //音声データ一時退避用の領域を確保し、audioClipからのデータを格納
            float[] soundData = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(soundData, 0);

            //新しい音声データ領域を確保し、positonの分だけ格納できるサイズにする。
            float[] newData = new float[position * audioClip.channels];

            //positionの分だけデータをコピー
            for (int i = 0; i < newData.Length; i++)
            {
                newData[i] = soundData[i];
            }

            //新しいAudioClipのインスタンスを生成し、音声データをセット
            AudioClip newClip = AudioClip.Create(audioClip.name, position, audioClip.channels, audioClip.frequency, false);
            newClip.SetData(newData, 0);

            //audioClipを新しいものに差し替え
            audioClip = newClip;
            
            //再生時間
            audioSource.clip = audioClip;
        }
    }

    ///-----------------------------------------------------------
    /// <summary>再生</summary>
    ///-----------------------------------------------------------
    public int Play()
    {
        //音声データ存在確認
        if (audioClip == null)
        {
            Debug.Log("音声データが見つかりません。");
            return 0;
        }
        Debug.Log("find!");
        //再生
        audioSource.clip = audioClip;
        int vol = GetVolume(audioClip);
        return vol; 
    }

    int GetVolume(AudioClip clip) {
        float[] data = new float[clip.samples];
        clip.GetData(data, 0);
        float a = 0;
        foreach(float s in data) {
            a += Mathf.Abs(s);
        }
        return (int)a*10;
    }

    // #endregion


}

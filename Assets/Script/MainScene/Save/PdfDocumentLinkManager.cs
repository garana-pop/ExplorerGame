using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PDFファイル名と次にアクティブにするオブジェクトの関連付けを管理するクラス
/// インスペクターで設定するか、特定のPDFファイル名に基づいて自動的に関連付けを行います
/// </summary>
public class PdfDocumentLinkManager : MonoBehaviour
{
    [System.Serializable]
    public class PdfNextObjectMapping
    {
        [Tooltip("PDFファイル名（例：警察記録.pdf）")]
        public string pdfFileName;

        [Tooltip("このPDFが完了したときにアクティブにするオブジェクト")]
        public GameObject nextObject;
    }

    [Header("PDFファイルのリンク設定")]
    [Tooltip("各PDFファイルと次にアクティブにするオブジェクトのマッピング")]
    [SerializeField] private List<PdfNextObjectMapping> customMappings = new List<PdfNextObjectMapping>();

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool useDefaultMappings = true;

    // シングルトンインスタンス
    private static PdfDocumentLinkManager _instance;
    public static PdfDocumentLinkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PdfDocumentLinkManager>(FindObjectsInactive.Include);

                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("PdfDocumentLinkManager");
                    _instance = go.AddComponent<PdfDocumentLinkManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // 内部マッピングキャッシュ
    private Dictionary<string, GameObject> mappingCache = new Dictionary<string, GameObject>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
            return;
        }

        _instance = this;

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }


        // マッピングの初期化
        InitializeMappings();
    }

    // マッピングの初期化
    private void InitializeMappings()
    {
        if (isInitialized) return;

        mappingCache.Clear();

        // カスタムマッピングの適用
        foreach (var mapping in customMappings)
        {
            if (!string.IsNullOrEmpty(mapping.pdfFileName) && mapping.nextObject != null)
            {
                mappingCache[mapping.pdfFileName] = mapping.nextObject;
                if (debugMode)
                    Debug.Log($"カスタムマッピングを追加: {mapping.pdfFileName} -> {mapping.nextObject.name}");
            }
        }

        // デフォルトマッピングの適用（要件に基づく）
        if (useDefaultMappings)
        {
            ApplyDefaultMappings();
        }

        isInitialized = true;
    }

    // 要件に基づくデフォルトマッピングの適用
    private void ApplyDefaultMappings()
    {
        // 初期化時は各オブジェクトを見つけるだけで、実際のマッピングはオブジェクトが見つかった場合のみ行う
        GameObject diagnosisPdf = FindObjectByFileName("記録ファイル-診断書.pdf");
        GameObject accidentPdf = FindObjectByFileName("記録ファイル-事故報告書.pdf");
        GameObject wishFolder = FindObjectByName("FolderButton_5 (願い)");
        GameObject fatherLetterPdf = FindObjectByFileName("願いファイル-父親からの手紙.pdf");
        GameObject daughterWishFile = FindObjectByFileName("願いファイル-娘からのお願い");

        // 見つかったオブジェクトをマッピングに追加
        if (diagnosisPdf != null)
            mappingCache["警察記録.pdf"] = diagnosisPdf;

        if (accidentPdf != null)
            mappingCache["診断書.pdf"] = accidentPdf;

        if (wishFolder != null)
            mappingCache["事故報告書.pdf"] = wishFolder;

        if (fatherLetterPdf != null)
            mappingCache["被害者の手記.pdf"] = fatherLetterPdf;

        if (daughterWishFile != null)
            mappingCache["父親からの手紙.pdf"] = daughterWishFile;

        if (debugMode)
        {
            foreach (var kvp in mappingCache)
            {
                Debug.Log($"マッピング: {kvp.Key} -> {kvp.Value.name}");
            }
        }
    }

    // ファイル名でGameObjectを検索（最適化のためFindFirstObjectByTypeを使用）
    private GameObject FindObjectByFileName(string fileName)
    {
        // ファイル名を含むFileOpenコンポーネントを検索
        foreach (var fileOpen in FindObjectsByType<FileOpen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fileOpen.gameObject.name == fileName || fileOpen.gameObject.name.Contains(fileName))
                return fileOpen.gameObject;
        }
        return null;
    }

    // オブジェクト名でGameObjectを検索
    private GameObject FindObjectByName(string objectName)
    {
        var objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in objects)
        {
            if (obj.name == objectName || obj.name.Contains(objectName))
                return obj;
        }
        return null;
    }

    // PDFファイル名に基づいて次にアクティブにするオブジェクトを取得
    public GameObject GetNextObjectForPdf(string pdfFileName)
    {
        if (string.IsNullOrEmpty(pdfFileName)) return null;

        // まだ初期化されていない場合は初期化
        if (!isInitialized)
            InitializeMappings();

        // マッピングキャッシュから検索
        if (mappingCache.TryGetValue(pdfFileName, out GameObject nextObject))
        {
            return nextObject;
        }

        return null;
    }

    // マッピングを動的に追加
    public void AddMapping(string pdfFileName, GameObject nextObject)
    {
        if (string.IsNullOrEmpty(pdfFileName) || nextObject == null) return;

        mappingCache[pdfFileName] = nextObject;

        if (debugMode)
            Debug.Log($"マッピングを動的に追加: {pdfFileName} -> {nextObject.name}");
    }
}
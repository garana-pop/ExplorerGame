using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FileOpen : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject infoPanel; // �\������p�l���i�C���X�y�N�^�[�Őݒ�j
    [SerializeField] private Canvas draggingCanvas; // �őO�ʗpCanvas�i�C���X�y�N�^�[�Őݒ�j
    [SerializeField] private GameObject overlay; // ������u���b�N����I�[�o�[���C�i�C���X�y�N�^�[�Őݒ�j

    // �p�Y����������p�̎Q��
    private PdfDocumentManager pdfDocManager;
    private ImageRevealer imageRevealer;
    private TxtPuzzleManager txtPuzzleManager;

    private RectTransform panelRectTransform; // �p�l����RectTransform
    private Transform originalParent; // �p�l���̌��̐e���L�^

    private void Awake()
    {
        // DraggingCanvas�̐ݒ�m�F�Ǝ擾
        if (draggingCanvas == null)
        {
            draggingCanvas = GameObject.Find("DraggingCanvas")?.GetComponent<Canvas>();
            if (draggingCanvas == null)
            {
                Debug.LogError("DraggingCanvas��������܂���B�C���X�y�N�^�[�Őݒ肵�Ă�������");
            }
        }

        // �I�[�o�[���C�̐ݒ�m�F
        if (overlay == null)
        {
            Debug.LogWarning("Overlay���ݒ肳��Ă��܂���B����u���b�N���@�\���܂���");
        }
        else
        {
            overlay.SetActive(false); // ������ԂŔ�A�N�e�B�u
        }

        // �p�l���̏����ݒ�
        if (infoPanel != null)
        {
            panelRectTransform = infoPanel.GetComponent<RectTransform>();
            if (panelRectTransform == null)
            {
                Debug.LogError("InfoPanel��RectTransform������܂���");
            }
            originalParent = infoPanel.transform.parent; // ���̐e���L�^
            SetActiveRecursive(infoPanel, false); // �q���܂߂Ĕ�A�N�e�B�u��
        }

        // �p�Y���i�s�Ǘ��N���X�ւ̎Q�Ƃ��擾
        pdfDocManager = GetComponentInChildren<PdfDocumentManager>(true);
        imageRevealer = GetComponentInChildren<ImageRevealer>(true);
        txtPuzzleManager = GetComponentInChildren<TxtPuzzleManager>(true);
    }

    private void Start()
    {
        // TxtPuzzleManager�̎Q�Ƃ��擾
        txtPuzzleManager = GetComponentInChildren<TxtPuzzleManager>(true);
    }

    // TxtPuzzleManager�ւ̃A�N�Z�T���\�b�h��ǉ�
    public TxtPuzzleManager GetTxtPuzzleManager()
    {
        return txtPuzzleManager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && infoPanel != null && !eventData.dragging)
        {
            // �p�l�����J���O��TxtPuzzleManager�ւ̎Q�Ƃ�ێ�
            txtPuzzleManager = infoPanel.GetComponentInChildren<TxtPuzzleManager>(true);

            SetActiveRecursive(infoPanel, true);
            infoPanel.transform.SetParent(draggingCanvas.transform, false);
            CenterPanelOnScreen();

            // TxtPuzzleManager���݊m�F�ƍĐڑ�����������
            if (txtPuzzleManager != null)
            {
                // �p�l���\�����ɋ����I�ɍă`�F�b�N�iTXT��p�p�l���̏ꍇ�j
                if (infoPanel.name.Contains("TXT") || infoPanel.name.EndsWith(".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    // �����x�������Ċm���ɓK�p�iTXT�p�l���ŗL�����j
                    StartCoroutine(DelayedPuzzleCheck(txtPuzzleManager));
                }
            }

            if (overlay != null)
            {
                overlay.SetActive(true);
            }
        }
    }
    // �V�K�ǉ�: TXT�p�Y����Ԃ�x���`�F�b�N���邽�߂̃R���[�`��
    private IEnumerator DelayedPuzzleCheck(TxtPuzzleManager puzzleManager)
    {
        yield return new WaitForSeconds(0.5f);

        // �p�Y���}�l�[�W���[��������ԂȂ�ForceCorrectState���Ăяo��
        if (puzzleManager.IsPuzzleCompleted())
        {
            puzzleManager.Invoke("ForceCorrectStateForAllAreas", 0.2f);

            // ����ɏ����x��Č���
            puzzleManager.Invoke("VerifyCorrectStateForAllAreas", 1.2f);
        }
    }

    // �q�I�u�W�F�N�g���܂߂ăA�N�e�B�u��Ԃ�ύX����w���p�[���\�b�h
    private void SetActiveRecursive(GameObject obj, bool state)
    {
        obj.SetActive(state);
        foreach (Transform child in obj.transform)
        {
            SetActiveRecursive(child.gameObject, state);
        }
    }

    // �p�l������ʒ����ɔz�u���郁�\�b�h
    private void CenterPanelOnScreen()
    {
        if (panelRectTransform == null || draggingCanvas == null) return;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        panelRectTransform.position = screenCenter;
    }

    // �p�l���̌��̐e���擾���郁�\�b�h�iFileClose�p�j
    public Transform GetOriginalParent()
    {
        return originalParent;
    }

    // �p�Y�����������Ă��邩�m�F���郁�\�b�h
    private bool IsPuzzleCompleted()
    {
        if (pdfDocManager != null && pdfDocManager.IsDocumentCompleted())
            return true;

        if (imageRevealer != null && imageRevealer.IsImageRevealed())
            return true;

        if (txtPuzzleManager != null && txtPuzzleManager.IsPuzzleCompleted())
            return true;

        return false;
    }
    public void ClosePanel()
    {
        if (infoPanel != null)
        {
            // PDF�}�l�[�W���[�𖾎��I�Ɏ擾
            PdfDocumentManager pdfManager = infoPanel.GetComponentInChildren<PdfDocumentManager>(true);
            bool pdfCompleted = false;
            GameObject nextFolder = null;

            // PDF���������Ă��邩�m�F���A���̃t�H���_�[���擾
            if (pdfManager != null)
            {
                pdfCompleted = pdfManager.IsDocumentCompleted();

                // ���̃t�H���_�[�̎Q�Ƃ𒼐ڎ擾�i���t���N�V�����������j
                if (pdfCompleted)
                {
                    // PDF�}�l�[�W���[�Ɏ��̃t�H���_�[�𒼐ڃA�N�e�B�u������悤�v��
                    pdfManager.EnsureNextFolderActive();
                }
            }

            // TXT�}�l�[�W���[�̊m�F�i�����R�[�h�j
            TxtPuzzleManager txtManager = infoPanel.GetComponentInChildren<TxtPuzzleManager>(true);
            bool txtCompleted = txtManager != null && txtManager.IsPuzzleCompleted();

            // �ʏ�̃p�l������鏈��
            infoPanel.transform.SetParent(originalParent, false);
            SetActiveRecursive(infoPanel, false);

            if (overlay != null)
            {
                overlay.SetActive(false);
            }

            // PDF�����Ŏ��t�H���_�[�̎Q�Ƃ��Ȃ��ꍇ�A����PDF�}�l�[�W���[�ɖ₢���킹
            if (pdfCompleted && nextFolder == null && pdfManager != null)
            {
                // �C���FPDF�}�l�[�W���[�Ɏ��̃t�H���_���ăA�N�e�B�x�[�g����悤�w��
                StartCoroutine(DelayedPdfManagerActivation(pdfManager));
            }

            // TXT�����̏ꍇ�i�����̃��W�b�N�j
            if (txtCompleted && nextFolder == null && txtManager != null)
            {
                nextFolder = txtManager.GetNextFolder();
                if (nextFolder != null)
                {
                    StartCoroutine(ReactivateNextFolder(nextFolder));
                }
            }

            // �Q�[���̏�Ԃ�ۑ����Ċ������m���ɂ���
            if ((pdfCompleted || txtCompleted) && GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }
        }
    }
    private IEnumerator DelayedPdfManagerActivation(PdfDocumentManager pdfManager)
    {
        // ���̏�������������܂ŏ����ҋ@
        yield return new WaitForSeconds(0.1f);

        // PDF�}�l�[�W���[�Ɏ��̃t�H���_�[���m���ɗL��������悤�w��
        if (pdfManager != null)
        {
            pdfManager.EnsureNextFolderActive();
        }
    }
    private IEnumerator ReactivateNextFolder(GameObject nextFolder)
    {
        // ���������܂őҋ@
        yield return new WaitForSeconds(0.1f);

        if (nextFolder != null)
        {
            // ��ɃA�N�e�B�u��Ԃ��m�F
            bool wasActive = nextFolder.activeSelf;

            // �m���ɃA�N�e�B�u�ɂ���
            nextFolder.SetActive(true);
            //Debug.Log($"���̃t�H���_�[���ăA�N�e�B�u��: {nextFolder.name}, �ȑO�̃A�N�e�B�u���: {wasActive}");

            // FolderButtonScript��FolderActivationGuard�̗�����ݒ�
            FolderButtonScript folderScript = nextFolder.GetComponent<FolderButtonScript>();
            if (folderScript == null)
                folderScript = nextFolder.GetComponentInParent<FolderButtonScript>();

            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
                folderScript.SetVisible(true);

                // �t�@�C���p�l�����m���ɕ\��
                if (folderScript.filePanel != null && !folderScript.filePanel.activeSelf)
                {
                    folderScript.filePanel.SetActive(true);
                }

                //Debug.Log($"�t�H���_�[�{�^���X�N���v�g���X�V: {folderScript.GetFolderName()}");
            }

            FolderActivationGuard guard = nextFolder.GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }
        }
    }

    // �p�Y���������ɊJ�������ׂ��t�H���_�[�������I�ɃA�N�e�B�u�ɂ���
    private void EnsureFoldersActive()
    {
        // PDF�h�L�������g�}�l�[�W���[�̊m�F
        if (pdfDocManager != null && pdfDocManager.IsDocumentCompleted())
        {
            ActivateNextFolder(pdfDocManager.gameObject);
        }

        // �摜���r�[���[�̊m�F
        if (imageRevealer != null && imageRevealer.IsImageRevealed())
        {
            ActivateNextFolder(imageRevealer.gameObject);
        }

        // �e�L�X�g�p�Y���}�l�[�W���[�̊m�F
        if (txtPuzzleManager != null && txtPuzzleManager.IsPuzzleCompleted())
        {
            ActivateNextFolder(txtPuzzleManager.gameObject);
        }
    }

    // �ΏۃR���|�[�l���g����NextFolderOrFile���擾���A�A�N�e�B�u�ɂ���
    private void ActivateNextFolder(GameObject obj)
    {
        var fields = obj.GetType().GetFields(System.Reflection.BindingFlags.Instance |
                                              System.Reflection.BindingFlags.Public |
                                              System.Reflection.BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.Name.Contains("nextFolder") || field.Name.Contains("NextFolder"))
            {
                var nextFolder = field.GetValue(obj.GetComponent(obj.GetType())) as GameObject;
                if (nextFolder != null)
                {
                    nextFolder.SetActive(true);
                    Debug.Log($"�t�H���_�[ {nextFolder.name} �������I�ɃA�N�e�B�u�ɂ��܂���");
                }
            }
        }
    }
}
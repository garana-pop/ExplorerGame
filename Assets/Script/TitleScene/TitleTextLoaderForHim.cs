using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// �Q�[�����[�h����afterChangeToHisFuture�t���O���`�F�b�N����
/// TitleTextChangerForHim�N���X��newTitleText�̒l��TitleContainer�ɕ\������N���X
/// </summary>
public class TitleTextLoaderForHim : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�\���Ώۂ�TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("�ʏ펞�̃^�C�g���e�L�X�g")]
    [SerializeField] private string normalTitleText = "�u�肢�v�̋L��";

    [Tooltip("afterChangeToHisFuture=true���̃^�C�g���e�L�X�g")]
    [SerializeField] private string changedTitleText = "�u�ށv�̖���";

    [Header("TitleTextChangerForHim�Q��")]
    [Tooltip("TitleTextChangerForHim�ւ̎Q�Ɓi�I�v�V�����j")]
    [SerializeField] private TitleTextChangerForHim titleTextChangerForHim;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceChangedTitle = false; // �e�X�g�p�̋����ύX

    private void Awake()
    {
        // TextMeshPro�R���|�[�l���g�̎����擾
        if (titleText == null)
        {
            titleText = GetComponent<TMP_Text>();
            if (titleText == null)
            {
                titleText = GetComponentInChildren<TMP_Text>();
            }
        }

        if (titleText == null)
        {
            Debug.LogError("TitleTextLoaderForHim: TextMeshPro�R���|�[�l���g��������܂���B�C���X�y�N�^�[�Őݒ肵�Ă��������B");
            enabled = false;
            return;
        }

        // TitleTextChangerForHim�̎�������
        if (titleTextChangerForHim == null)
        {
            titleTextChangerForHim = FindFirstObjectByType<TitleTextChangerForHim>();
        }

        // TitleTextChangerForHim����ݒ�l���擾
        if (titleTextChangerForHim != null)
        {
            // �ύX��e�L�X�g���擾�i���t���N�V�������g�p�j
            var newTitleField = titleTextChangerForHim.GetType().GetField("newTitleText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (newTitleField != null)
            {
                string newTitleValue = newTitleField.GetValue(titleTextChangerForHim) as string;
                if (!string.IsNullOrEmpty(newTitleValue))
                {
                    changedTitleText = newTitleValue;
                }
            }

            // ���[�h���͌��ʉ��𖳌���
            titleTextChangerForHim.SetSoundEnabled(false);
        }
    }

    private void Start()
    {
        // �����x�������Ċm����GameSaveManager������������Ă�����s
        StartCoroutine(LoadAndApplyTitleDelayed());
    }

    /// <summary>
    /// �x����Ƀ^�C�g���e�L�X�g��ǂݍ���œK�p
    /// </summary>
    private IEnumerator LoadAndApplyTitleDelayed()
    {
        // GameSaveManager�̏�������҂�
        yield return new WaitForSeconds(0.1f);

        LoadAndApplyTitle();
    }

    /// <summary>
    /// �Z�[�u�f�[�^����t���O��ǂݍ���Ń^�C�g���e�L�X�g��K�p
    /// </summary>
    private void LoadAndApplyTitle()
    {
        // afterChangeToLast�t���O��true�̏ꍇ�͏������X�L�b�v
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("TitleTextLoaderForHim: afterChangeToLast��true�̂��ߏ������X�L�b�v���܂�");
            return;
        }

        bool shouldChangeTitle = false;

        // �f�o�b�O���[�h�ł̋����ύX
        if (debugMode && forceChangedTitle)
        {
            shouldChangeTitle = true;
            if (debugMode) Debug.Log("TitleTextLoaderForHim: �f�o�b�O���[�h�ŋ����I�Ƀ^�C�g����ύX");
        }
        else
        {
            // �����̃t���O���`�F�b�N
            bool herMemoryFlag = GetAfterChangeToHerMemoryFlag();
            bool hisFutureFlag = GetAfterChangeToHisFutureFlag();

            // �����̃t���O��true�̏ꍇ�̂݃^�C�g����ύX
            shouldChangeTitle = herMemoryFlag && hisFutureFlag;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoaderForHim: afterChangeToHerMemory = {herMemoryFlag}");
                Debug.Log($"TitleTextLoaderForHim: afterChangeToHisFuture = {hisFutureFlag}");
                Debug.Log($"TitleTextLoaderForHim: ���t���O���� = {shouldChangeTitle}");
            }
        }

        // �^�C�g���e�L�X�g��ݒ�
        if (shouldChangeTitle)
        {
            titleText.text = changedTitleText;
            if (debugMode) Debug.Log($"TitleTextLoaderForHim: �^�C�g�����u{changedTitleText}�v�ɕύX���܂���");
        }
    }

    /// <summary>
    /// afterChangeToHerMemory�t���O���擾
    /// </summary>
    private bool GetAfterChangeToHerMemoryFlag()
    {
        // GameSaveManager����擾�����݂�
        if (GameSaveManager.Instance != null)
        {
            try
            {
                // ���t���N�V�������g�p���ăZ�[�u�f�[�^���擾
                var saveDataField = GameSaveManager.Instance.GetType().GetField("currentSaveData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (saveDataField != null)
                {
                    var saveData = saveDataField.GetValue(GameSaveManager.Instance);
                    if (saveData != null)
                    {
                        var flagField = saveData.GetType().GetField("afterChangeToHerMemory");
                        if (flagField != null)
                        {
                            bool flagValue = (bool)flagField.GetValue(saveData);
                            if (debugMode) Debug.Log($"TitleTextLoaderForHim: afterChangeToHerMemory�t���O = {flagValue}");
                            return flagValue;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (debugMode) Debug.LogError($"TitleTextLoaderForHim: �t���O�擾�G���[: {e.Message}");
            }
        }

        // �t���O���擾�ł��Ȃ��ꍇ��false��Ԃ�
        if (debugMode) Debug.LogWarning("TitleTextLoaderForHim: afterChangeToHerMemory�t���O���擾�ł��܂���ł���");
        return false;
    }

    /// <summary>
    /// afterChangeToHisFuture�t���O���擾
    /// </summary>
    private bool GetAfterChangeToHisFutureFlag()
    {
        // GameSaveManager����擾�����݂�
        if (GameSaveManager.Instance != null)
        {
            // GameSaveManager�Ƀ��\�b�h�����݂���Ɖ���
            // ���ۂ̃��\�b�h���ɍ��킹�ĕύX���Ă�������
            try
            {
                // ���t���N�V�������g�p���ăZ�[�u�f�[�^���擾
                var saveDataField = GameSaveManager.Instance.GetType().GetField("currentSaveData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (saveDataField != null)
                {
                    var saveData = saveDataField.GetValue(GameSaveManager.Instance);
                    if (saveData != null)
                    {
                        var flagField = saveData.GetType().GetField("afterChangeToHisFuture");
                        if (flagField != null)
                        {
                            bool flagValue = (bool)flagField.GetValue(saveData);
                            if (debugMode) Debug.Log($"TitleTextLoaderForHim: afterChangeToHisFuture�t���O = {flagValue}");
                            return flagValue;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (debugMode) Debug.LogError($"TitleTextLoaderForHim: �t���O�擾�G���[: {e.Message}");
            }
        }

        // �t���O���擾�ł��Ȃ��ꍇ��false��Ԃ�
        if (debugMode) Debug.LogWarning("TitleTextLoaderForHim: afterChangeToHisFuture�t���O���擾�ł��܂���ł���");
        return false;
    }

    /// <summary>
    /// �蓮�Ń^�C�g�����ēǂݍ��݁i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Reload Title")]
    public void ReloadTitle()
    {
        LoadAndApplyTitle();
    }

    /// <summary>
    /// �t���O�̏�Ԃ��蓮�Őݒ�i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Toggle Changed Title")]
    public void ToggleChangedTitle()
    {
        forceChangedTitle = !forceChangedTitle;
        LoadAndApplyTitle();
    }
}
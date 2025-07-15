using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// �Q�[�����[�h����afterChangeToHerMemory�AafterChangeToHisFuture�AafterChangeToLast�t���O���`�F�b�N����
/// ���ׂ�true�̏ꍇ�A�^�C�g����"Thanks for playing the game."�ɕ\������N���X
/// </summary>
public class TitleTextLoaderForMonologueScene : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�\���Ώۂ�TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("���ׂẴt���O��true���̃^�C�g���e�L�X�g")]
    [SerializeField] private string finalTitleText = "Thanks for playing the game.";

    [Header("TitleTextChangerForMonologueScene�Q��")]
    [Tooltip("TitleTextChangerForMonologueScene�ւ̎Q�Ɓi�I�v�V�����j")]
    [SerializeField] private TitleTextChangerForMonologueScene titleTextChangerForMonologue;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceAllFlagsTrue = false; // �e�X�g�p�̋����ύX

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
            Debug.LogError("TitleTextLoaderForMonologueScene: TextMeshPro�R���|�[�l���g��������܂���B�C���X�y�N�^�[�Őݒ肵�Ă��������B");
            enabled = false;
            return;
        }

        // TitleTextChangerForMonologueScene�̎�������
        if (titleTextChangerForMonologue == null)
        {
            titleTextChangerForMonologue = FindFirstObjectByType<TitleTextChangerForMonologueScene>();
        }

        // ���ʉ����m���ɖ�����
        if (titleTextChangerForMonologue != null)
        {
            // �����ɖ�����
            titleTextChangerForMonologue.SetSoundEnabled(false);

            // �R���[�`���Œx�����s���ǉ��i�O�̂��߁j
            StartCoroutine(DisableSoundDelayed());
        }
    }

    private IEnumerator DisableSoundDelayed()
    {
        yield return null; // 1�t���[���ҋ@

        if (titleTextChangerForMonologue != null)
        {
            titleTextChangerForMonologue.SetSoundEnabled(false);
            if (debugMode) Debug.Log("TitleTextLoaderForMonologueScene: �x�����s�Ō��ʉ��𖳌������܂���");
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
        bool shouldChangeFinal = false;

        // �f�o�b�O���[�h�ł̋����ύX
        if (debugMode && forceAllFlagsTrue)
        {
            shouldChangeFinal = true;
            if (debugMode) Debug.Log("TitleTextLoaderForMonologueScene: �f�o�b�O���[�h�ŋ����I�Ƀ^�C�g����ύX");
        }
        else
        {
            // 3�̃t���O���`�F�b�N
            bool herMemoryFlag = GetAfterChangeToHerMemoryFlag();
            bool hisFutureFlag = GetAfterChangeToHisFutureFlag();
            bool lastFlag = GetAfterChangeToLastFlag();

            // ���ׂẴt���O��true�̏ꍇ�̂݃^�C�g����ύX
            shouldChangeFinal = herMemoryFlag && hisFutureFlag && lastFlag;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoaderForMonologueScene: afterChangeToHerMemory = {herMemoryFlag}");
                Debug.Log($"TitleTextLoaderForMonologueScene: afterChangeToHisFuture = {hisFutureFlag}");
                Debug.Log($"TitleTextLoaderForMonologueScene: afterChangeToLast = {lastFlag}");
                Debug.Log($"TitleTextLoaderForMonologueScene: �S�t���O���� = {shouldChangeFinal}");
            }

        }

        // ���ׂẴt���O��true�̏ꍇ�̂݃^�C�g����ύX ������ȊO�͉������Ȃ�
        if (shouldChangeFinal)
        {
            string textToApply = finalTitleText;
            titleText.text = textToApply;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoaderForMonologueScene: �^�C�g���� '{finalTitleText}' �ɐݒ肵�܂���");
            }
        }
    }

    /// <summary>
    /// afterChangeToHerMemory�t���O���擾
    /// </summary>
    private bool GetAfterChangeToHerMemoryFlag()
    {
        // GameSaveManager����擾
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }

        // �t���O���擾�ł��Ȃ��ꍇ��false��Ԃ�
        if (debugMode) Debug.LogWarning("TitleTextLoaderForMonologueScene: GameSaveManager�����݂��Ȃ����߁AafterChangeToHerMemory�t���O���擾�ł��܂���ł���");
        return false;
    }

    /// <summary>
    /// afterChangeToHisFuture�t���O���擾
    /// </summary>
    private bool GetAfterChangeToHisFutureFlag()
    {
        // GameSaveManager����擾
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHisFutureFlag();
        }

        // �t���O���擾�ł��Ȃ��ꍇ��false��Ԃ�
        if (debugMode) Debug.LogWarning("TitleTextLoaderForMonologueScene: GameSaveManager�����݂��Ȃ����߁AafterChangeToHisFuture�t���O���擾�ł��܂���ł���");
        return false;
    }

    /// <summary>
    /// afterChangeToLast�t���O���擾
    /// </summary>
    private bool GetAfterChangeToLastFlag()
    {
        // �܂�TitleTextChangerForMonologueScene�̐ÓI�t���O���`�F�b�N
        if (TitleTextChangerForMonologueScene.IsTitleChanged())
        {
            return true;
        }

        // GameSaveManager����擾
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToLastFlag();
        }

        // �t���O���擾�ł��Ȃ��ꍇ��false��Ԃ�
        if (debugMode) Debug.LogWarning("TitleTextLoaderForMonologueScene: GameSaveManager�����݂��Ȃ����߁AafterChangeToLast�t���O���擾�ł��܂���ł���");
        return false;
    }
}
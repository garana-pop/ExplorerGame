using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DraggingCanvas���̎q�I�u�W�F�N�g�̊K�w�������Ǘ�����N���X
/// ���ɐݒ��ʕ\�����ɁAFilePanel��Overlay��SettingsPanel�̏�����ۏ؂���
/// </summary>
public class HierarchyOrderManager : MonoBehaviour
{
    [Header("�Q�Ɛݒ�")]
    [SerializeField] private Canvas draggingCanvas;
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject settingsPanel;

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    // MainSceneSettingsManager�ւ̎Q��
    private MainSceneSettingsManager settingsManager;

    // �L���ȃt�@�C���p�l�����X�g�i�L���b�V���j
    private List<GameObject> filePanels = new List<GameObject>();

    private void Awake()
    {
        // �Q�Ƃ̏�����
        InitializeReferences();
    }

    private void Start()
    {
        // MainSceneSettingsManager�Ƃ̘A�g�iStart���ōs���j
        ConnectToSettingsManager();
    }

    private void OnEnable()
    {
        // �I�u�W�F�N�g���L���ɂȂ������ɂ��Q�Ƃ��m�F
        if (draggingCanvas == null || overlay == null || settingsPanel == null)
        {
            InitializeReferences();
        }
    }

    /// <summary>
    /// �K�v�ȎQ�Ƃ�����������
    /// </summary>
    private void InitializeReferences()
    {
        // DraggingCanvas���ݒ肳��Ă��Ȃ��ꍇ�͎�������
        if (draggingCanvas == null)
        {
            draggingCanvas = GameObject.Find("DraggingCanvas")?.GetComponent<Canvas>();
            if (draggingCanvas == null)
            {
                Debug.LogError("HierarchyOrderManager: DraggingCanvas��������܂���B");
                enabled = false;
                return;
            }
        }

        // Overlay���ݒ肳��Ă��Ȃ��ꍇ�͎�������
        if (overlay == null)
        {
            overlay = draggingCanvas.transform.Find("Overlay")?.gameObject;
            if (overlay == null)
            {
                Debug.LogWarning("HierarchyOrderManager: Overlay��������܂���BDraggingCanvas/Overlay���쐬���Ă��������B");
            }
        }

        // SettingsPanel���ݒ肳��Ă��Ȃ��ꍇ�͎�������
        if (settingsPanel == null)
        {
            // DraggingCanvas���́uSettingsPanel�v�Ƃ������O�̃I�u�W�F�N�g������
            settingsPanel = FindObjectWithNameInCanvas("SettingsPanel");
            if (settingsPanel == null)
            {
                Debug.LogWarning("HierarchyOrderManager: SettingsPanel��������܂���B�蓮�Őݒ肵�Ă��������B");
            }
        }

        // �t�@�C���p�l���̃��X�g���ŐV��
        UpdateFilePanelsList();

        // ������Ԃł�Overlay���A�N�e�B�u�ɂ���
        SetOverlayActive(false);

        if (debugMode)
        {
            LogCurrentHierarchy("��������̃q�G�����L�[");
        }
    }

    /// <summary>
    /// MainSceneSettingsManager���������ĘA�g����
    /// </summary>
    private void ConnectToSettingsManager()
    {
        settingsManager = FindFirstObjectByType<MainSceneSettingsManager>();

        if (settingsManager != null)
        {
            // SettingsManager�̃��\�b�h��T���Ď��s���ɐڑ����邽�߂̏���
            // ���˂��g�p���ăv���C�x�[�g���\�b�h�ɃA�N�Z�X�i�񐄏�������ނ𓾂Ȃ��ꍇ�j
            var settingsType = settingsManager.GetType();

            // ShowSettings��HideSettings���Ď�����
            // Update���ŏ�ԕω������o����悤�ɕύX
            if (debugMode)
            {
                Debug.Log("HierarchyOrderManager: MainSceneSettingsManager�ƘA�g���܂���");
            }
        }
        else
        {
            Debug.LogWarning("HierarchyOrderManager: MainSceneSettingsManager��������܂���");
        }
    }

    private void Update()
    {
        // settingsManager�����݂��AsettingsPanel���擾�ł��Ă���ꍇ
        if (settingsManager != null && settingsPanel != null)
        {
            // SettingsPanel�̃A�N�e�B�u��ԂɊ�Â���Overlay�𐧌�
            bool settingsActive = settingsPanel.activeInHierarchy;

            // SettingsPanel���A�N�e�B�u�ŁAOverlay����A�N�e�B�u�̏ꍇ
            if (settingsActive && overlay != null && !overlay.activeInHierarchy)
            {
                // �ݒ��ʂ��\�����ꂽ���K�w�����𒲐�����Overlay���A�N�e�B�u��
                AdjustHierarchyOrderForSettings();
                SetOverlayActive(true);

                if (debugMode)
                {
                    Debug.Log("HierarchyOrderManager: �ݒ��ʂ��\������܂����BOverlay���A�N�e�B�u�ɂ��܂��B");
                }
            }
            // SettingsPanel����A�N�e�B�u�ŁAOverlay���A�N�e�B�u�̏ꍇ
            else if (!settingsActive && overlay != null && overlay.activeInHierarchy)
            {
                // �ݒ��ʂ�����ꂽ��Overlay���A�N�e�B�u��
                SetOverlayActive(false);

                if (debugMode)
                {
                    Debug.Log("HierarchyOrderManager: �ݒ��ʂ������܂����BOverlay���A�N�e�B�u�ɂ��܂��B");
                }
            }
        }
    }

    /// <summary>
    /// Overlay�̕\��/��\����ݒ�
    /// </summary>
    /// <param name="active">�\������ꍇ��true�A��\���̏ꍇ��false</param>
    public void SetOverlayActive(bool active)
    {
        if (overlay != null && overlay.activeSelf != active)
        {
            overlay.SetActive(active);

            if (debugMode)
            {
                Debug.Log($"HierarchyOrderManager: Overlay��{(active ? "�\��" : "��\��")}�ɂ��܂���");
            }
        }
    }

    /// <summary>
    /// �ݒ�p�l���\���O�ɌĂ΂��K�w�����������\�b�h
    /// </summary>
    public void AdjustHierarchyOrderForSettings()
    {
        if (draggingCanvas == null)
        {
            Debug.LogError("HierarchyOrderManager: DraggingCanvas���ݒ肳��Ă��܂���");
            return;
        }

        // �t�@�C���p�l���̃��X�g���ŐV��
        UpdateFilePanelsList();

        if (debugMode)
        {
            LogCurrentHierarchy("�����O�̃q�G�����L�[");
        }

        // FilePanel���ł����ɔz�u
        foreach (GameObject filePanel in filePanels)
        {
            if (filePanel != null && filePanel.activeInHierarchy)
            {
                filePanel.transform.SetAsFirstSibling();
                if (debugMode)
                {
                    Debug.Log($"HierarchyOrderManager: {filePanel.name}���ŉ����Ɉړ����܂���");
                }
            }
        }

        // Overlay�𒆊Ԃɔz�u�i���݂���ꍇ�j
        if (overlay != null)
        {
            // FilePanel�̏�ASettingsPanel�̉��ɔz�u
            int targetIndex = 0;
            // �A�N�e�B�u��FilePanel�̐����J�E���g
            foreach (GameObject panel in filePanels)
            {
                if (panel != null && panel.activeInHierarchy)
                {
                    targetIndex++;
                }
            }

            overlay.transform.SetSiblingIndex(targetIndex);
            if (debugMode)
            {
                Debug.Log($"HierarchyOrderManager: Overlay���ʒu {targetIndex} �Ɉړ����܂���");
            }
        }

        // SettingsPanel���ł���ɔz�u�i���݂���ꍇ�j
        if (settingsPanel != null)
        {
            settingsPanel.transform.SetAsLastSibling();
            if (debugMode)
            {
                Debug.Log("HierarchyOrderManager: SettingsPanel���ŏ㕔�Ɉړ����܂���");
            }
        }

        if (debugMode)
        {
            LogCurrentHierarchy("������̃q�G�����L�[");
        }
    }

    /// <summary>
    /// DraggingCanvas���̌��݂�FilePanel��T�����čX�V
    /// </summary>
    private void UpdateFilePanelsList()
    {
        if (draggingCanvas == null) return;

        filePanels.Clear();

        for (int i = 0; i < draggingCanvas.transform.childCount; i++)
        {
            Transform child = draggingCanvas.transform.GetChild(i);
            // FilePanel���܂ޖ��O�̃I�u�W�F�N�g��T��
            if (child.name.Contains("FilePanel"))
            {
                filePanels.Add(child.gameObject);
                if (debugMode)
                {
                    Debug.Log($"HierarchyOrderManager: �t�@�C���p�l�������o: {child.name}");
                }
            }
        }
    }

    /// <summary>
    /// DraggingCanvas���œ���̖��O���܂ރI�u�W�F�N�g������
    /// </summary>
    private GameObject FindObjectWithNameInCanvas(string nameContains)
    {
        if (draggingCanvas == null) return null;

        for (int i = 0; i < draggingCanvas.transform.childCount; i++)
        {
            Transform child = draggingCanvas.transform.GetChild(i);
            if (child.name.Contains(nameContains))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// ���݂̃q�G�����L�[��Ԃ����O�ɏo�́i�f�o�b�O�p�j
    /// </summary>
    private void LogCurrentHierarchy(string message)
    {
        if (!debugMode || draggingCanvas == null) return;

        Debug.Log($"--- {message} ---");

        for (int i = 0; i < draggingCanvas.transform.childCount; i++)
        {
            Transform child = draggingCanvas.transform.GetChild(i);
            Debug.Log($"�ʒu {i}: {child.name} (�A�N�e�B�u: {child.gameObject.activeInHierarchy})");
        }

        Debug.Log("------------------------");
    }

    /// <summary>
    /// �蓮�Ńq�G�����L�[�������Ăяo�����߂̃p�u���b�N���\�b�h
    /// </summary>
    public void ManualAdjustHierarchy()
    {
        AdjustHierarchyOrderForSettings();
    }

    /// <summary>
    /// �ݒ��ʂ�����ꂽ�Ƃ��ɌĂяo����郁�\�b�h
    /// </summary>
    public void OnSettingsClosed()
    {
        SetOverlayActive(false);

        if (debugMode)
        {
            Debug.Log("HierarchyOrderManager: �ݒ��ʃN���[�Y���̏��������s���܂���");
        }
    }

    private void OnDestroy()
    {
        // ���\�[�X�̃N���[���A�b�v�i�K�v�ɉ����āj
    }
}
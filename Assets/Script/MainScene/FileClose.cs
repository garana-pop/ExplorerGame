using UnityEngine;
using UnityEngine.UI;

public class FileClose : MonoBehaviour
{
    [SerializeField] private FileOpen fileOpen; // FileOpen�ւ̎Q��
    [SerializeField] private float unlockDelay = 2.0f; // �{�^�����b�N�����̒x�����ԁi�b�j

    private Button closeButton; // ���̃X�N���v�g���t����Button
    private bool isLocked = false; // �{�^�������b�N����Ă��邩�ǂ���

    private void Awake()
    {
        // Button�R���|�[�l���g���擾
        closeButton = GetComponent<Button>();
        if (closeButton == null)
        {
            Debug.LogError("Button�R���|�[�l���g��������܂���");
            return;
        }

        // FileOpen���ݒ肳��Ă��邩�m�F
        if (fileOpen == null)
        {
            //Debug.LogError("FileOpen���C���X�y�N�^�[�Őݒ肳��Ă��܂���");
            return;
        }

        // Button�̃N���b�N�C�x���g��ݒ�
        closeButton.onClick.AddListener(ClosePanel);
    }

    // �p�l������鏈��
    private void ClosePanel()
    {
        // �{�^�������b�N����Ă���ꍇ�͉������Ȃ�
        if (isLocked)
        {
            Debug.Log("�{�^���̓��b�N����Ă��邽�߁A���鑀��͖�������܂���");
            return;
        }

        // ShockEffect�����`�F�b�N
        PdfDocumentManager pdfManager = GetComponentInParent<PdfDocumentManager>();

        // TXT�p�Y���̊������������`�F�b�N
        TxtPuzzleManager txtManager = fileOpen.GetComponentInChildren<TxtPuzzleManager>(true);
        if (txtManager != null && txtManager.IsProcessingCompletion())
        {
            Debug.Log("TXT�p�Y�������������̂��߁A���鑀��͖�������܂���");
            return;
        }

        if (fileOpen != null)
        {
            // �ǉ�: �t�@�C�������O��PdfManager�̏�Ԃ����Z�b�g
            PdfDocumentManager pdfDocManager = fileOpen.GetComponentInChildren<PdfDocumentManager>(true);

            // �t�@�C���p�l�������
            fileOpen.ClosePanel();
        }
    }

    // �{�^�������b�N����i�O������Ăяo���\�j
    public void LockButton()
    {
        isLocked = true;
        closeButton.interactable = false;
    }

    // �{�^���̃��b�N����������i�O������Ăяo���\�j
    public void UnlockButton()
    {
        isLocked = false;
        closeButton.interactable = true;
    }

    // �x���t���Ń{�^���̃��b�N����������
    public void UnlockButtonDelayed()
    {
        Invoke("UnlockButton", unlockDelay);
    }
}
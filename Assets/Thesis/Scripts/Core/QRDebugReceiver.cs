using UnityEngine;

public class QRDebugReceiver : MonoBehaviour
{
    public void OnDecoded(string raw)
    {
        Debug.Log($"[QRDebugReceiver] RAW=<{raw}> LEN={(raw == null ? -1 : raw.Length)}");
    }
}
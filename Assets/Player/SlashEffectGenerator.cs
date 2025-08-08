using System.Collections;
using UnityEngine;

public class SlashEffectGenerator : MonoBehaviour {
  [SerializeField] private GameObject slashPrefab;
  [SerializeField] private float slashTime = 0.5f;

  public void SpawnSlash(Vector3 startPos, Quaternion rot, Vector3 dir) {
    GameObject slashEffect = Instantiate(slashPrefab, startPos, rot);
    StartCoroutine(MoveSlash(slashEffect, dir));
  }
  
  private IEnumerator MoveSlash(GameObject slashEffect, Vector3 dir) {
    float time = 0;
    Vector3 startPos = slashEffect.transform.position;
    Vector3 endPos = startPos + dir * 2f;
    
    while (time < slashTime) {
      slashEffect.transform.position = Vector3.Lerp(startPos, endPos, time);
      time += Time.deltaTime;
      yield return null;
    }
    Destroy(slashEffect.gameObject);
  }
}
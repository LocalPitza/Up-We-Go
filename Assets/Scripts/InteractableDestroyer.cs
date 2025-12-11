using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class InteractableDestroyer : MonoBehaviour
{
    [SerializeField] AudioClip audioData;
    [SerializeField] AudioSource audios;
    [SerializeField] private LayerMask destroyableLayers;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object is on the "Interactable" layer
        if (((1 << other.gameObject.layer) & destroyableLayers) != 0)
        {
            Destroy(other.gameObject);
            audios.clip = audioData;
            audios.Play();
        }
    }
}

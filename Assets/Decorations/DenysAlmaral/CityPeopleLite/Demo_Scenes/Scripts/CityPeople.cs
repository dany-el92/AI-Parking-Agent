using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityPeople
{
    public class CityPeople : MonoBehaviour
    {
        private AnimationClip[] myClips;
        private Animator animator;

        void Start()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                myClips = animator.runtimeAnimatorController.animationClips;
                PlayAnyClip();
                StartCoroutine(ShuffleClips());
            }

        }

        // void PlayAnyClip()
        // {
        //     var cl = myClips[Random.Range(0, myClips.Length)];
        //     animator.CrossFadeInFixedTime(cl.name, 1.0f, 0, Random.value * cl.length);
        // }
        void PlayAnyClip()
        {
            var cl = myClips[Random.Range(0, myClips.Length)];
            var stateName = GetStateNameFromClipName(cl.name);
            animator.CrossFadeInFixedTime(stateName, 1.0f, 0, Random.value * cl.length);
        }

        string GetStateNameFromClipName(string clipName)
        {
            // Qui dovresti implementare la logica per mappare il nome del clip di animazione al nome dello stato corrispondente.
            // Per ora, restituiremo semplicemente il nome del clip, supponendo che corrisponda al nome dello stato.
            return clipName;
        }

        IEnumerator ShuffleClips()
        {
            while (true)
            {
                yield return new WaitForSeconds(15.0f + Random.value * 5.0f);
                PlayAnyClip();
            }
        }

    }
}

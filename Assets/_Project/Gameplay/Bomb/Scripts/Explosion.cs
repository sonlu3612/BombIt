using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets._Project.Gameplay.Bomb.Scripts
{
    public class Explosion : MonoBehaviour
    {
        private Animator anim;

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public void Play()
        {
            anim.Play(0, 0, 0f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ActorPoolingExample
{
    public class Actor
    {
        public float lifespan;
        public Color color;
        public Vector2 position;
    }

    public class ActorViewExample : InstanceView<Actor>
    {
        [SerializeField] new private SpriteRenderer renderer;

        protected override void Configure()
        {
            renderer.color = config.color;
        }

        public override void Refresh()
        {
            transform.localPosition = config.position;
        }
    }
}

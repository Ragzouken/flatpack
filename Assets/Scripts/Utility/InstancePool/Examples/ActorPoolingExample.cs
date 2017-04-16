using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ActorPoolingExample
{
    public class ActorPoolingExample : MonoBehaviour
    {
        [SerializeField]
        private InstancePoolSetup actorsSetup;
        private InstancePool<Actor> actorViews;

        private List<Actor> actors = new List<Actor>();

        private void Awake()
        {
            actorViews = actorsSetup.Finalise<Actor>(sort: false);
        }

        private void Update()
        {
            if (actors.Count < 20)
            {
                actors.Add(new Actor
                {
                    color = new Color(Random.value, Random.value, Random.value, 1),
                    lifespan = Random.value * 5,
                    position = new Vector2(Random.Range(-1f, 1f),
                                           Random.Range(-1f, 1f)),
                });
            }

            foreach (Actor actor in actors)
            {
                actor.lifespan -= Time.deltaTime;
                actor.position += new Vector2(Random.Range(-1f, 1f),
                                              Random.Range(-1f, 1f)) * Time.deltaTime;
            }

            actors.RemoveAll(actor => actor.lifespan <= 0);

            actorViews.SetActive(actors);
            actorViews.Refresh();
        }
    }
}

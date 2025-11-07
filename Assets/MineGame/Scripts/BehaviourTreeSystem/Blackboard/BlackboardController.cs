using UnityEngine;

namespace BlackboardSystem {
    public class BlackboardController : MonoBehaviour {
        readonly Blackboard blackboard = new();
        readonly Arbiter arbiter = new();

        void Awake() {
            blackboard.Debug();
        }
        
        public Blackboard GetBlackboard() => blackboard;
        
        public void RegisterExpert(IExpert expert) => arbiter.RegisterExpert(expert);
        public void DeregisterExpert(IExpert expert) => arbiter.DeregisterExpert(expert);

        void Update() {
            // Execute all agreed actions from the current iteration
            foreach (var action in arbiter.BlackboardIteration(blackboard)) {
                action();
            }
        }
    }
}
using System.Collections.Concurrent;

namespace FenUISharp
{
    public class Dispatcher
    {
        private ulong _currentTick = 0;
        private readonly ConcurrentQueue<DispatcherCall> dispatcherCalls = new();
        private readonly ConcurrentDictionary<string, bool> uniqueIds = new();

        internal void UpdateQueue()
        {
            _currentTick++;

            // Process all items currently in queue
            var itemsToProcess = new List<DispatcherCall>();

            while (dispatcherCalls.TryDequeue(out var call))
            {
                itemsToProcess.Add(call);
            }

            try
            {
                foreach (var call in itemsToProcess)
                {
                    if (call != null)
                    {
                        bool shouldExecute = false;
                        bool shouldRequeue = false;

                        switch (call.mode)
                        {
                            case 0: // Tick-based delay
                                if (_currentTick - call.tickAtCall >= call.ticksLater)
                                {
                                    shouldExecute = true;
                                }
                                else
                                {
                                    shouldRequeue = true;
                                }
                                break;

                            case 1: // Time-based delay
                                if (DateTime.Now.Subtract(call.timeAtCall).TotalSeconds >= call.sLater)
                                {
                                    shouldExecute = true;
                                }
                                else
                                {
                                    shouldRequeue = true;
                                }
                                break;

                            case 2: // Immediate execution
                                shouldExecute = true;
                                break;
                        }

                        if (shouldExecute)
                        {
                            call.action?.Invoke();
                            // Remove from unique IDs if it had one
                            if (call.uniqueId != null)
                            {
                                uniqueIds.TryRemove(call.uniqueId, out _);
                            }
                        }
                        else if (shouldRequeue)
                        {
                            // Put it back in the queue for next update
                            dispatcherCalls.Enqueue(call);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while processing update queue: " + e.Message);

                // Clear everything on error
                while (dispatcherCalls.TryDequeue(out _)) { }
                uniqueIds.Clear();
            }
        }

        /// <summary>
        /// Will call the action on the next update ensuring everything is properly ran on the logic loop inside the update call
        /// </summary>
        /// <param name="action">The action that wants to be invoked</param>
        public void Invoke(Action action)
        {
            dispatcherCalls.Enqueue(new DispatcherCall(action));
        }

        /// <summary>
        /// Will call the action on the next update ensuring everything is properly ran on the logic loop inside the update call. 
        /// It also checks if the ID already exists, and will ensure there are no duplicates inside one queue
        /// </summary>
        /// <param name="action">The action that wants to be invoked</param>
        /// <param name="id">The unique id</param>
        public void InvokeWithID(Action action, string id)
        {
            if (uniqueIds.TryAdd(id, true))
            {
                dispatcherCalls.Enqueue(new DispatcherCall(action, id));
            }
        }

        public void InvokeLater(Action action, ulong ticks)
        {
            dispatcherCalls.Enqueue(new DispatcherCall(action, _currentTick, ticks));
        }

        public void InvokeLater(Action action, float seconds)
        {
            dispatcherCalls.Enqueue(new DispatcherCall(action, seconds));
        }

        /// <summary>
        /// Gets the current number of pending calls in the queue
        /// </summary>
        public int PendingCallsCount => dispatcherCalls.Count;

        /// <summary>
        /// Gets the current number of unique IDs being tracked
        /// </summary>
        public int UniqueIdsCount => uniqueIds.Count;
    }

    internal class DispatcherCall
    {
        public Action? action;
        public DateTime timeAtCall;
        public string? uniqueId;
        public float sLater = 0;
        public ulong tickAtCall = 0;
        public ulong ticksLater = 0;
        public int mode = 0;

        public DispatcherCall(Action action, ulong tickAtCall, ulong ticksLater)
        {
            this.action = action;
            this.tickAtCall = tickAtCall;
            this.ticksLater = ticksLater;
            mode = 0;
        }

        public DispatcherCall(Action action, float sLater)
        {
            this.action = action;
            this.timeAtCall = DateTime.Now;
            this.sLater = sLater;
            mode = 1;
        }

        public DispatcherCall(Action action, string? uniqueID = null)
        {
            this.uniqueId = uniqueID;
            this.action = action;
            mode = 2;
        }
    }
}
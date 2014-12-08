using System;
using System.Collections.Generic;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.ScriptLibrary
{
    public class ScriptableEventManager
    {
        #region Custom Events For Scriptability
        public delegate void CustomEventOccuredDelegate(string identifier, params object[] data);
        protected Dictionary<string, List<CustomEventOccuredDelegate>> CustomEventListeners = new Dictionary<string, List<CustomEventOccuredDelegate>>();

        /// <summary>
        /// Adds given 'method' to listener list for specified 'identifier'.
        /// </summary>
        /// <param name="identifier">Identifier of the custom event that will be listened to.</param>
        /// <param name="method">Method that will be called if specified custom event occurs.</param>
        /// <returns>Number of the methods listening specified custom event!</returns>
        public int ListenCustomEvent(string identifier, CustomEventOccuredDelegate method)
        {
            if(!CustomEventListeners.ContainsKey(identifier))
                CustomEventListeners.Add(identifier, new List<CustomEventOccuredDelegate>());

            CustomEventListeners[identifier].Add(method);
            return CustomEventListeners[identifier].Count;
        }

        /// <summary>
        /// Removes first occurence of given 'method' from listener list for specified 'identifier'.
        /// </summary>
        /// <param name="identifier">Identifier of the custom event that will be forgotten to.</param>
        /// <param name="method">Method that will be called if specified custom event occurs.</param>
        /// <returns>Number of the methods listening specified custom event!</returns>
        public int ForgetCustomEvent(string identifier, CustomEventOccuredDelegate method)
        {
            if (!CustomEventListeners.ContainsKey(identifier))
                return (int)ScriptError.CustomEventNotRegisteredYet;
            
            CustomEventListeners[identifier].Remove(method);
            return CustomEventListeners[identifier].Count; 
        }

        /// <summary>
        /// Invokes pre-registered methods for given 'identifier' with given 'data' 
        /// </summary>
        /// <param name="identifier">Identifier of the custom event that will be invoked.</param>
        /// <param name="data">Data that will be passed to invoked methods.</param>
        /// <returns>Number of the methods listening specified custom event!</returns>
        public int FireOnCustomEventHappened(string identifier, params object[] data)
        {
            if (!CustomEventListeners.ContainsKey(identifier))
                return (int)ScriptError.CustomEventNotRegisteredYet;

            foreach (CustomEventOccuredDelegate method in CustomEventListeners[identifier])
            {
                method.Invoke(identifier, data);
            }
            return CustomEventListeners[identifier].Count;
        }
        #endregion

        public static event Action<Connection> OnUserLoginDataRetrieved;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DesignPattern
{
    public class ObservableProperty<T>
    {
        private T value;
        public event Action<T> OnValueChanged;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                OnValueChanged?.Invoke(value);
            }
        }

        public ObservableProperty(T initialValue)
        {
            value = initialValue;
        }
    }
}


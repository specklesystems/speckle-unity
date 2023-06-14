using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Speckle.Core.Api;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Wrappers.Selection
{
    /// <summary>
    /// Reusable <see langword="abstract"/> serializable type that abstracts
    /// the fetching of <typeparamref name="TOption"/> objects.
    /// And exposes an <see cref="Array"/> of <see cref="Options"/>
    /// with serialised selection.
    /// </summary>
    /// <typeparam name="TOption"></typeparam>
    [Serializable]
    public abstract class OptionSelection<TOption>
        where TOption : class
    {
        [SerializeField] private int selectedIndex = -1;

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                selectedIndex = value;
                OnSelectionChange?.Invoke();
            }
        }

        public TOption? Selected
        {
            get
            {
                if (Options is null) return null;
                if (SelectedIndex < 0 || SelectedIndex >= Options.Length) return null;
                return Options[SelectedIndex];
            }
        }

        public TOption[] Options { get; protected set; } = Array.Empty<TOption>();
        public Action? OnSelectionChange { get; set; }

        public abstract Client? Client { get; }

        [return: NotNullIfNotNull("value")]
        protected abstract string? KeyFunction(TOption? value);

        public abstract void RefreshOptions();

        protected void GenerateOptions(IList<TOption> source, Func<TOption, int, bool> isDefault)
        {
            List<TOption> optionsToAdd = new (source.Count);
            int defaultOption = -1;
            int index = 0;
            foreach (TOption? a in source)
            {
                if (a == null) continue;
                optionsToAdd.Add(a);
                if (isDefault(a, index)) defaultOption = index;
                index++;
            }

            TOption? currentSelected = Selected;
            bool selectionOutOfRange = SelectedIndex < 0 || SelectedIndex >= optionsToAdd.Count;
            if (selectionOutOfRange
                || (currentSelected != null
                    && KeyFunction(currentSelected) != KeyFunction(optionsToAdd[SelectedIndex])))
            {
                selectedIndex = defaultOption;
            }

            Options = optionsToAdd.ToArray();
            //Debug.Log($"{this.GetType()} updated");
            OnSelectionChange?.Invoke();
        }
    }
}

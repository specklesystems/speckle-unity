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
    /// And exposes an list of <see cref="Options"/>
    /// with serialised selection.
    /// </summary>
    /// <typeparam name="TOption"></typeparam>
    [Serializable]
    public abstract class OptionSelection<TOption>
        where TOption : class
    {
        public IReadOnlyList<TOption> Options { get; protected set; } = Array.Empty<TOption>();

        private Dictionary<string, int>? _indexMap;

        [SerializeField]
        private string? selectedId;

        public TOption? Selected
        {
            get
            {
                if (selectedId == null)
                    return null;

                TryGetOption(selectedId, out var value);
                return value;
            }
            set
            {
                selectedId = KeyFunction(value);
                OnSelectionChange?.Invoke();
            }
        }

        public bool TryGetOption(string key, [NotNullWhen(true)] out TOption? value)
        {
            if (_indexMap is not null && _indexMap.TryGetValue(key, out int index))
            {
                value = Options[index];
                return true;
            }

            value = null;
            return false;
        }

        public Action? OnSelectionChange { get; set; }

        public abstract Client? Client { get; }

        [return: NotNullIfNotNull("value")]
        protected abstract string? KeyFunction(TOption? value);

        public abstract void RefreshOptions();

        protected void GenerateOptions(
            IReadOnlyCollection<TOption?> source,
            Func<TOption, int, bool> isDefault
        )
        {
            List<TOption> optionsToAdd = new(source.Count);
            Dictionary<string, int> indexMap = new(source.Count);
            string? defaultOption = null;
            int index = 0;
            foreach (TOption? a in source)
            {
                if (a == null)
                    continue;

                var key = KeyFunction(a);
                optionsToAdd.Add(a);
                indexMap.Add(key, index);

                if (isDefault(a, index))
                    defaultOption = key;

                index++;
            }

            string? currentSelected = selectedId;
            if (currentSelected is null || !indexMap.ContainsKey(currentSelected))
            {
                selectedId = defaultOption;
            }

            Options = optionsToAdd;
            _indexMap = indexMap;
            OnSelectionChange?.Invoke();
        }
    }
}

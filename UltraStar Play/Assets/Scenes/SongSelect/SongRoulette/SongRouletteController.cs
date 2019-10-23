﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SongRouletteController : MonoBehaviour
{
    public SongRouletteItem songRouletteItemPrefab;
    public List<RouletteItemPlaceholder> rouletteItemPlaceholders;

    private List<RouletteItemPlaceholder> activeRouletteItemPlaceholders = new List<RouletteItemPlaceholder>();
    private RouletteItemPlaceholder centerItem;
    private int centerItemIndex;

    private List<SongMeta> songs = new List<SongMeta>();
    public SongMeta SelectedSong { get; private set; }
    private int selectedSongIndex;

    private List<SongRouletteItem> songRouletteItems = new List<SongRouletteItem>();

    public bool showRouletteItems;

    public SongSelectSceneController SongSelectSceneController { get; set; }

    void Start()
    {
        activeRouletteItemPlaceholders = new List<RouletteItemPlaceholder>(rouletteItemPlaceholders);
        centerItemIndex = (int)Math.Floor(rouletteItemPlaceholders.Count / 2f);
        centerItem = rouletteItemPlaceholders[centerItemIndex];

        if (!showRouletteItems)
        {
            foreach (RouletteItemPlaceholder item in rouletteItemPlaceholders)
            {
                Image image = item.GetComponentInChildren<Image>();
                image.enabled = false;
            }
        }
    }

    void Update()
    {
        if (songs.Count == 0)
        {
            return;
        }

        UpdateSongRouletteItems();
    }

    private void UpdateSongRouletteItems()
    {
        List<SongRouletteItem> usedSongRouletteItems = new List<SongRouletteItem>();

        // Spawn roulette items for songs to be displayed (i.e. selected song and its surrounding songs)
        int activeCenterIndex = activeRouletteItemPlaceholders.IndexOf(centerItem);
        foreach (RouletteItemPlaceholder rouletteItem in activeRouletteItemPlaceholders)
        {
            int index = activeRouletteItemPlaceholders.IndexOf(rouletteItem);
            int activeCenterDistance = index - activeCenterIndex;
            int songIndex = selectedSongIndex + activeCenterDistance;
            SongMeta songMeta = GetSongAtIndex(songIndex);

            // Get or create SongRouletteItem for the song at the index
            SongRouletteItem songRouletteItem = songRouletteItems.Where(it => it.SongMeta == songMeta).FirstOrDefault();
            if (songRouletteItem == null)
            {
                songRouletteItem = CreateSongRouletteItem(songMeta, rouletteItem);
            }

            // Update target
            if (songRouletteItem.TargetRouletteItem != rouletteItem)
            {
                songRouletteItem.TargetRouletteItem = rouletteItem;
            }
            usedSongRouletteItems.Add(songRouletteItem);
        }

        // Remove unused items
        foreach (SongRouletteItem songRouletteItem in new List<SongRouletteItem>(songRouletteItems))
        {
            if (!usedSongRouletteItems.Contains(songRouletteItem))
            {
                RemoveSongRouletteItem(songRouletteItem);
            }
        }
    }

    private void RemoveSongRouletteItem(SongRouletteItem songRouletteItem)
    {
        songRouletteItem.TargetRouletteItem = null;
        songRouletteItems.Remove(songRouletteItem);
    }

    private SongRouletteItem CreateSongRouletteItem(SongMeta songMeta, RouletteItemPlaceholder rouletteItem)
    {
        SongRouletteItem item = Instantiate(songRouletteItemPrefab);
        item.transform.SetParent(transform);
        item.SongMeta = songMeta;
        item.RectTransform.anchorMin = rouletteItem.RectTransform.anchorMin;
        item.RectTransform.anchorMax = rouletteItem.RectTransform.anchorMax;
        item.RectTransform.sizeDelta = Vector2.zero;
        item.RectTransform.anchoredPosition = Vector2.zero;
        item.TargetRouletteItem = rouletteItem;
        item.AnimTime = 1;
        item.ScaleTime = 1;

        item.GetComponent<Button>().onClick.AddListener(() => SelectSong(songMeta));

        songRouletteItems.Add(item);
        return item;
    }

    private void FindActiveRouletteItems()
    {
        List<int> activeRouletteItemIndexes = new List<int>();
        for (int i = 1; i <= songs.Count && i <= rouletteItemPlaceholders.Count; i++)
        {
            // Map the sequence (1,2,3,4,5,6,7,...), which is i,
            // to (0, -1, 1, -2, 2, -3, 3, ...), which is centerDistance.
            // This way, the available roulette items are added inside out.
            int centerDistance = ((int)Math.Floor(i / 2f)) * ((i % 2 == 0) ? -1 : 1);
            int availableItemIndex = centerItemIndex + centerDistance;
            activeRouletteItemIndexes.Add(availableItemIndex);
        }

        activeRouletteItemIndexes.Sort();
        activeRouletteItemPlaceholders.Clear();
        foreach (int index in activeRouletteItemIndexes)
        {
            if (index < rouletteItemPlaceholders.Count)
            {
                activeRouletteItemPlaceholders.Add(rouletteItemPlaceholders[index]);
            }
        }
    }

    private void UpdateRouletteItemsActiveState()
    {
        foreach (RouletteItemPlaceholder item in rouletteItemPlaceholders)
        {
            item.gameObject.SetActive(false);
        }

        foreach (RouletteItemPlaceholder item in activeRouletteItemPlaceholders)
        {
            item.gameObject.SetActive(true);
        }
    }

    public void SetSongs(List<SongMeta> songMetas)
    {
        songs = new List<SongMeta>(songMetas);
        if (songs.Count > 0)
        {
            selectedSongIndex = 0;
            SelectedSong = songs[selectedSongIndex];
            FindActiveRouletteItems();
            UpdateRouletteItemsActiveState();
        }
        else
        {
            SelectedSong = null;
            selectedSongIndex = -1;
            RemoveAllSongRouletteItems();
        }
        SongSelectSceneController.OnSongSelected(SelectedSong, selectedSongIndex, songs);
    }

    private void RemoveAllSongRouletteItems()
    {
        foreach (SongRouletteItem songRouletteItem in songRouletteItems)
        {
            songRouletteItem.TargetRouletteItem = null;
        }
        songRouletteItems.Clear();
    }

    public void SelectSong(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        SongMeta matchingSong = songs.Where(song => song.Title == songMeta.Title).FirstOrDefault();
        if (matchingSong != null)
        {
            SelectedSong = matchingSong;
            selectedSongIndex = songs.IndexOf(SelectedSong);

            if (SongSelectSceneController != null)
            {
                SongSelectSceneController.OnSongSelected(SelectedSong, selectedSongIndex, songs);
            }
        }
    }

    public void SelectNextSong()
    {
        int indexOfSelectedSong = songs.IndexOf(SelectedSong);
        int nextIndex;
        if (indexOfSelectedSong == -1)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = indexOfSelectedSong + 1;
        }
        SongMeta nextSong = GetSongAtIndex(nextIndex);
        SelectSong(nextSong);
    }

    public void SelectPreviousSong()
    {
        int indexOfSelectedSong = songs.IndexOf(SelectedSong);
        int nextIndex;
        if (indexOfSelectedSong == -1)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = indexOfSelectedSong - 1;
        }
        SongMeta nextSong = GetSongAtIndex(nextIndex);
        SelectSong(nextSong);
    }

    public void SelectRandomSong()
    {
        int songIndex = new System.Random().Next(0, songs.Count - 1);
        Debug.Log("next random song index: " + songIndex);
        SongMeta nextSong = GetSongAtIndex(songIndex);
        SelectSong(nextSong);
    }

    private SongMeta GetSongAtIndex(int nextIndex)
    {
        if (songs.Count == 0)
        {
            return null;
        }
        int wrappedIndex = (nextIndex < 0) ? nextIndex + songs.Count : nextIndex;
        int wrappedIndexModulo = wrappedIndex % songs.Count;
        SongMeta song = songs[wrappedIndexModulo];
        return song;
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UIListManager : MonoBehaviour
{
    // References to UI elements
    public Button openListButton;        // Button to trigger the list
    public Button randomItemButton;      // Button to select a random item
    public GameObject scrollView;       // The entire ScrollView (it will toggle visibility)
    public GameObject scrollViewContent; // The Content of the ScrollView (the parent of items)
    public GameObject itemPrefab;       // The prefab for each item in the list

    // List to store item data
    public List<ItemData> itemList = new List<ItemData>();

    // Keep track of the instantiated item GameObjects to highlight them later
    private List<GameObject> instantiatedItems = new List<GameObject>();

    void Start()
    {
        // Ensure that the buttons are properly set up
        if (openListButton != null)
        {
            openListButton.onClick.AddListener(ToggleList); // Add listener to toggle the list
        }

        if (randomItemButton != null)
        {
            randomItemButton.onClick.AddListener(SelectRandomItem); // Add listener to choose random item
        }

        // Example of populating the item list with sample data (images and text)
        itemList.Add(new ItemData("Ana Karenjina", "Lav Tolstoj", Resources.Load<Sprite>("1")));
        itemList.Add(new ItemData("Koreni", "Dobrica Ćosić", Resources.Load<Sprite>("2")));
        itemList.Add(new ItemData("Majstor i Margarita", "Mihail Bulgakov", Resources.Load<Sprite>("3")));
        itemList.Add(new ItemData("The Haunter of the Dark", "H.P. Lovecraft", Resources.Load<Sprite>("4")));
        itemList.Add(new ItemData("The Secret History", "Donna Tartt", Resources.Load<Sprite>("5")));

        // Hide the ScrollView and Random button initially by setting them inactive
        if (scrollView != null)
        {
            scrollView.SetActive(false); // Hide the ScrollView when the game starts
        }

        if (randomItemButton != null)
        {
            randomItemButton.gameObject.SetActive(false); // Hide the Random button initially
        }
    }

    void ToggleList()
    {
        if (scrollView == null || scrollViewContent == null || itemPrefab == null)
        {
            Debug.LogError("ScrollView or its Content or ItemPrefab is not assigned!");
            return;
        }

        // Toggle the visibility of the ScrollView (show or hide it)
        if (scrollView.activeSelf)
        {
            // If the ScrollView is active, hide it
            scrollView.SetActive(false);
            randomItemButton.gameObject.SetActive(false); // Hide the Random button when ScrollView is hidden
        }
        else
        {
            // If the ScrollView is inactive, show it and populate the list
            scrollView.SetActive(true);
            randomItemButton.gameObject.SetActive(true); // Show the Random button when ScrollView is shown

            // Clear previous items in the ScrollView content
            foreach (Transform child in scrollViewContent.transform)
            {
                Destroy(child.gameObject);
            }

            // Instantiate new items in the ScrollView based on the itemList
            instantiatedItems.Clear();  // Clear the list of instantiated items to avoid adding them twice
            foreach (var item in itemList)
            {
                GameObject newItem = Instantiate(itemPrefab, scrollViewContent.transform);  // Instantiate the prefab
                instantiatedItems.Add(newItem); // Track the instantiated items

                if (newItem != null)
                {
                    // Set the text for the item
                    TMP_Text nameText = newItem.transform.GetChild(0).GetComponent<TMP_Text>();
                    if (nameText != null)
                    {
                        nameText.text = item.bookName; // Assign item text
                    }

                    TMP_Text authorText = newItem.transform.GetChild(1).GetComponent<TMP_Text>();
                    if (authorText != null)
                    {
                        authorText.text = item.authorName; // Assign item text
                    }

                    // Set the image for the item
                    Image itemImage = newItem.transform.GetChild(2).GetComponent<Image>();
                    if (itemImage != null && item.itemImage != null)
                    {
                        itemImage.sprite = item.itemImage; // Assign item image
                    }
                }
            }
        }
    }

    void SelectRandomItem()
    {
        if (instantiatedItems.Count == 0)
        {
            Debug.LogError("No items to select from!");
            return;
        }

        // First, reset all items to their normal state (remove highlight)
        foreach (var item in instantiatedItems)
        {
            ResetHighlight(item);
        }

        // Select a random item from the list of instantiated items
        int randomIndex = Random.Range(0, instantiatedItems.Count);
        GameObject selectedItem = instantiatedItems[randomIndex];

        // Highlight the selected item
        HighlightItem(selectedItem);
    }

    // Method to highlight an item (e.g., change background color with transparency)
    void HighlightItem(GameObject item)
    {
        if (item == null) return;

        // Find the highlight Image (child of the item prefab)
        Image highlightImage = item.transform.Find("Highlight").GetComponent<Image>();
        if (highlightImage != null)
        {
            Color highlightColor = new Color(1f, 1f, 0f, 0.5f); // Yellow with 50% transparency
            highlightImage.color = highlightColor; // Set the color to yellow with 50% transparency
        }
    }

    // Method to reset the highlight on an item
    void ResetHighlight(GameObject item)
    {
        if (item == null) return;

        // Find the highlight Image (child of the item prefab)
        Image highlightImage = item.transform.Find("Highlight").GetComponent<Image>();
        if (highlightImage != null)
        {
            Color defaultColor = new Color(1f, 1f, 1f, 0f); // White with 0% transparency (invisible)
            highlightImage.color = defaultColor; // Reset to fully transparent
        }
    }
}

[System.Serializable]
public class ItemData
{
    public string bookName;
    public string authorName;
    public Sprite itemImage;

    public ItemData(string name, string author, Sprite image)
    {
        bookName = name;
        authorName = author;
        itemImage = image;
    }
}

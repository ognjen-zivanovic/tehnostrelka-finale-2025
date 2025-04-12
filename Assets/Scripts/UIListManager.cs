using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.IO;
using UnityEngine.Serialization;

public class UIListManager : MonoBehaviour
{
    // References to UI elements
    public Button openListButton;
    public Button randomItemButton;
    public GameObject scrollView;
    public GameObject scrollViewContent;
    public GameObject itemPrefab;
    public GameObject cropBoxObject;
    public GameObject scanButton;

    // List to store item data
    public List<ItemData> itemList = new List<ItemData>();

    // Keep track of the instantiated item GameObjects
    private List<GameObject> instantiatedItems = new List<GameObject>();

    private string saveFilePath;

    void Start()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "itemList.json");

        // Ensure buttons are properly set up
        if (openListButton != null)
        {
            openListButton.onClick.AddListener(ToggleList);
        }

        if (randomItemButton != null)
        {
            randomItemButton.onClick.AddListener(SelectRandomItem);
        }

        // Load previously saved items
        LoadItemList();

        // Hide the ScrollView and Random button initially
        if (scrollView != null)
        {
            scrollView.SetActive(false);
        }

        if (randomItemButton != null)
        {
            randomItemButton.gameObject.SetActive(false);
        }
    }

    public void AddItemToList(string name, string author, Sprite image)
    {
        ItemData newItem = new ItemData(name, author, image);
        itemList.Add(newItem);
        SaveItemList();  // Save the list whenever a new item is added
    }

    void ToggleList()
    {
        if (scrollView == null || scrollViewContent == null || itemPrefab == null)
        {
            Debug.LogError("ScrollView or its Content or ItemPrefab is not assigned!");
            return;
        }

        if (scrollView.activeSelf)
        {
            scrollView.SetActive(false);
            randomItemButton.gameObject.SetActive(false);
            cropBoxObject.SetActive(true);
            scanButton.SetActive(true);
        }
        else
        {
            scrollView.SetActive(true);
            randomItemButton.gameObject.SetActive(true);
            cropBoxObject.SetActive(false);
            scanButton.SetActive(false);

            // Clear previous items in the ScrollView content
            foreach (Transform child in scrollViewContent.transform)
            {
                Destroy(child.gameObject);
            }

            // Instantiate new items in the ScrollView based on the itemList
            instantiatedItems.Clear();
            foreach (var item in itemList)
            {
                GameObject newItem = Instantiate(itemPrefab, scrollViewContent.transform);
                instantiatedItems.Add(newItem);

                if (newItem != null)
                {
                    TMP_Text nameText = newItem.transform.GetChild(0).GetComponent<TMP_Text>();
                    if (nameText != null)
                    {
                        nameText.text = item.bookName;
                    }

                    TMP_Text authorText = newItem.transform.GetChild(1).GetComponent<TMP_Text>();
                    if (authorText != null)
                    {
                        authorText.text = item.authorName;
                    }

                    Image itemImage = newItem.transform.GetChild(2).GetComponent<Image>();
                    if (itemImage != null && item.itemImage != null)
                    {
                        itemImage.sprite = item.itemImage;
                        itemImage.preserveAspect = true;
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

        foreach (var item in instantiatedItems)
        {
            ResetHighlight(item);
        }

        int randomIndex = Random.Range(0, instantiatedItems.Count);
        GameObject selectedItem = instantiatedItems[randomIndex];
        HighlightItem(selectedItem);
    }

    void HighlightItem(GameObject item)
    {
        if (item == null) return;

        Image highlightImage = item.transform.Find("Highlight").GetComponent<Image>();
        if (highlightImage != null)
        {
            Color highlightColor = new Color(1f, 1f, 0f, 0.5f);
            highlightImage.color = highlightColor;
        }
    }

    void ResetHighlight(GameObject item)
    {
        if (item == null) return;

        Image highlightImage = item.transform.Find("Highlight").GetComponent<Image>();
        if (highlightImage != null)
        {
            Color defaultColor = new Color(1f, 1f, 1f, 0f);
            highlightImage.color = defaultColor;
        }
    }

    // Save the itemList to a file in JSON format
    void SaveItemList()
    {
        List<ItemDataSerializable> serializableList = new List<ItemDataSerializable>();
        foreach (var item in itemList)
        {
            serializableList.Add(new ItemDataSerializable(item));
        }

        string json = JsonUtility.ToJson(new ItemListWrapper { items = serializableList });
        File.WriteAllText(saveFilePath, json);
    }

    // Load the itemList from a file
    void LoadItemList()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            ItemListWrapper wrapper = JsonUtility.FromJson<ItemListWrapper>(json);

            itemList.Clear();
            foreach (var serializableItem in wrapper.items)
            {
                Sprite sprite = LoadSpriteFromBase64(serializableItem.base64Image);
                ItemData item = new ItemData(serializableItem.bookName, serializableItem.authorName, sprite);
                itemList.Add(item);
            }
        }
        else
        {
            itemList.Add(new ItemData("Ana Karenjina", "Lav Tolstoj", Resources.Load<Sprite>("1")));
            itemList.Add(new ItemData("Koreni", "Dobrica Ćosić", Resources.Load<Sprite>("2")));
            itemList.Add(new ItemData("Majstor i Margarita", "Mihail Bulgakov", Resources.Load<Sprite>("3")));
            itemList.Add(new ItemData("The Haunter of the Dark", "H.P. Lovecraft", Resources.Load<Sprite>("4")));
            itemList.Add(new ItemData("The Secret History", "Donna Tartt", Resources.Load<Sprite>("5")));
            itemList.Add(new ItemData("Phantom", "Jo Nesbo", Resources.Load<Sprite>("6")));
        }
    }

    // Convert a base64 string to a Sprite
    Sprite LoadSpriteFromBase64(string base64String)
    {
        byte[] bytes = System.Convert.FromBase64String(base64String);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
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

[System.Serializable]
public class ItemDataSerializable
{
    public string bookName;
    public string authorName;
    public string base64Image;

    public ItemDataSerializable(ItemData item)
    {
        bookName = item.bookName;
        authorName = item.authorName;
        base64Image = ImageToBase64(item.itemImage);
    }

    // Convert Sprite to base64 string
    string ImageToBase64(Sprite sprite)
    {
        Texture2D texture = sprite.texture;
        byte[] bytes = texture.EncodeToPNG();
        return System.Convert.ToBase64String(bytes);
    }
}

[System.Serializable]
public class ItemListWrapper
{
    public List<ItemDataSerializable> items;
}

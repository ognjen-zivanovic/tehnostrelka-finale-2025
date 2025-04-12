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
    private string imagesFolderPath;

    void Start()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "itemList.json");
        imagesFolderPath = Path.Combine(Application.persistentDataPath, "images/");
        Debug.Log(saveFilePath);

        // Ensure images folder exists
        if (!Directory.Exists(imagesFolderPath))
        {
            Directory.CreateDirectory(imagesFolderPath);
        }

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
        ItemDataSerializable serializableItem = new ItemDataSerializable(newItem);
        SaveImageToFile(image, serializableItem.imagePath);
        SaveItemList();  // Save the list whenever a new item is added
    }

    void SaveImageToFile(Sprite image, string fileName)
    {
        string filePath = Path.Combine(imagesFolderPath, fileName);

        if (File.Exists(filePath))
        {
            return; // Image already exists, return existing path
        }

        // Save the image as PNG
        byte[] bytes = image.texture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
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
                        nameText.text = item.bookName + "\n" + item.authorName;
                    }

                    // TMP_Text authorText = newItem.transform.GetChild(1).GetComponent<TMP_Text>();
                    // if (authorText != null)
                    // {
                    //     authorText.text = item.authorName;
                    // }

                    Image itemImage = newItem.transform.GetChild(2).GetComponent<Image>();
                    if (itemImage != null)
                    {
                        itemImage.sprite = item.bookSprite;
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
                Sprite sprite = LoadSpriteFromFile(imagesFolderPath + serializableItem.imagePath);
                ItemData item = new ItemData(serializableItem.bookName, serializableItem.authorName, sprite);
                itemList.Add(item);
            }
        }
        else
        {
            // Add some default items if no file exists
            AddItemToList("Phantom", "Jo Nesbo", Resources.Load<Sprite>("6"));
            AddItemToList("The Secret History", "Donna Tartt", Resources.Load<Sprite>("5"));
            AddItemToList("The Haunter of the Dark", "H.P. Lovecraft", Resources.Load<Sprite>("4"));
            AddItemToList("Majstor i Margarita", "Mihail Bulgakov", Resources.Load<Sprite>("3"));
            AddItemToList("Koreni", "Dobrica Ćosić", Resources.Load<Sprite>("2"));
            AddItemToList("Ana Karenjina", "Lav Tolstoj", Resources.Load<Sprite>("1"));
        }
    }

    // Load a sprite from a file path
    Sprite LoadSpriteFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Image file not found at path: " + filePath);
            return null;
        }

        byte[] imageData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}

[System.Serializable]
public class ItemData
{
    public string bookName;
    public string authorName;
    public Sprite bookSprite; // Path to the image file

    public ItemData(string name, string author, Sprite sprite)
    {
        bookName = name;
        authorName = author;
        this.bookSprite = sprite;
    }
}

[System.Serializable]
public class ItemDataSerializable
{
    public string bookName;
    public string authorName;
    public string imagePath; // Path to the image file

    public ItemDataSerializable(ItemData item)
    {
        bookName = item.bookName;
        authorName = item.authorName;
        imagePath = item.bookName + "_" + item.authorName + ".png";
    }
}

[System.Serializable]
public class ItemListWrapper
{
    public List<ItemDataSerializable> items;
}

using UnityEngine;
using UnityEngine.SceneManagement; // 必须引入这个命名空间

public class MainMenuController : MonoBehaviour
{
    
    public void PlayGame()
    {
        SceneManager.LoadScene("Tutorial_Test");
    }

    // 顺便把退出游戏也写了，面日常实习时这也算配套规范
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("游戏已退出"); // 在编辑器里打包运行后有效
    }
}
using UnityEngine; // Debug.Log 등을 사용하려면 필요

// 예시: 공 종류를 나타내는 열거형 (필요에 맞게 수정하세요)
public enum BallType
{
    Default,
    Fireball,
    Iceball,
    LightningBall 
    // 필요에 따라 더 많은 공 종류 추가
}

/// <summary>
/// 씬 전환 시 공유될 데이터를 담는 클래스입니다.
/// MonoBehaviour를 상속하지 않는 일반 C# 클래스(POCO)입니다.
/// 이 클래스의 인스턴스는 VContainer의 상위 스코프(예: ProjectContext)에 등록되어야 합니다.
/// </summary>
public class GameSessionData
{
    /// <summary>
    /// 메인 메뉴 등에서 선택된 공의 종류입니다.
    /// </summary>
    public BallType SelectedBallType { get; set; } = BallType.Default; // 기본값 설정

    // 필요하다면 다른 씬 간 공유 데이터 추가
    // public string PlayerName { get; set; } 
    // public int SelectedDifficulty { get; set; }

    public GameSessionData()
    {
        // 초기화 로직 (필요 시)
        Debug.Log("GameSessionData 인스턴스 생성됨.");
    }

    public void Reset()
    {
        // 데이터 초기화 로직
        SelectedBallType = BallType.Default;
        // PlayerName = null;
        // SelectedDifficulty = 0;
        Debug.Log("GameSessionData 리셋됨.");
    }
} 
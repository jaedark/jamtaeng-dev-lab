from dataclasses import dataclass

@dataclass
class Incident:
    title: str
    severity: int
    resolved: bool = False

def summarize_incident(incident: Incident) -> str:
    status = "해결 완료" if incident.resolved else "처리 중"
    return f"{incident.title}: {status}"

if __name__ == "__main__":
    sample = Incident("생산 시스템 통신 오류", 3)
    print(summarize_incident(sample))

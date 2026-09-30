namespace Galashow.Trolley.Demo
{
    /// <summary>
    /// 데모용 문항 (docs/minigame-trolley.md 8절에서 발췌). 실제 문항은 API game_data로 받는다.
    /// </summary>
    public static class TrolleySampleDilemmas
    {
        public readonly struct Dilemma
        {
            public readonly string Id, Title, Description, A, ADescription, B, BDescription;

            public Dilemma(string id, string title, string description, string a, string aDescription, string b, string bDescription)
            {
                Id = id; Title = title; Description = description;
                A = a; ADescription = aDescription; B = b; BDescription = bDescription;
            }
        }

        public static readonly Dilemma[] All =
        {
            new Dilemma("chicken-vs-tteokbokki", "야식 트롤리",
                "트롤리가 방금 도착한 치킨 다섯 마리로 돌진 중! 레버를 당기면 내 최애 떡볶이 한 그릇 쪽으로 갑니다.",
                "치킨을 살린다", "떡볶이를 보낸다", "떡볶이를 지킨다", "치킨은 또 시키면 된다"),
            new Dilemma("mint-choco", "민초 공장",
                "트롤리가 민트초코 공장으로 달려갑니다. 레버를 당기면 하와이안 피자 공장으로 바뀝니다.",
                "민초를 지킨다", "하와이안 공장이 멈춘다", "하와이안을 지킨다", "민초 공장이 멈춘다"),
            new Dilemma("pour-or-dip", "탕수육 선로",
                "트롤리가 '부먹' 마을로 돌진 중. 레버를 당기면 '찍먹' 마을로 갑니다.",
                "부먹을 지킨다", "찍먹 마을로 보낸다", "찍먹을 지킨다", "부먹 마을로 보낸다"),
            new Dilemma("end-stream-button", "방종 버튼",
                "트롤리가 '방송 종료' 버튼을 누르러 갑니다. 레버를 당기면 '마이크 음소거' 버튼을 누릅니다.",
                "음소거가 낫다", "말 없이 방송 유지", "차라리 방종", "깔끔하게 끝낸다"),
            new Dilemma("wifi-or-aircon", "한여름의 선택",
                "한여름, 트롤리가 와이파이 공유기로 돌진 중. 레버를 당기면 에어컨이 부서집니다.",
                "와이파이를 지킨다", "더위는 참는다", "에어컨을 지킨다", "인터넷은 잠시 포기"),
            new Dilemma("lottery", "복권 선로",
                "트롤리가 '지금 당장 1억' 선로로 갑니다. 레버를 당기면 '평생 매달 100만 원' 선로로 바뀝니다.",
                "지금 1억", "당장 목돈", "평생 100만 원", "꾸준한 연금"),
            new Dilemma("time-machine", "시간 열차",
                "트롤리가 '10년 전으로 돌아가기' 역으로 갑니다. 레버를 당기면 '10년 뒤로 건너뛰기' 역으로 바뀝니다.",
                "과거로 간다", "후회를 바로잡는다", "미래로 간다", "궁금한 게 더 많다"),
            new Dilemma("cat-or-dog", "간식 창고",
                "트롤리가 고양이 간식 창고로 달려갑니다. 레버를 당기면 강아지 간식 창고로 바뀝니다.",
                "냥이 간식을 지킨다", "멍이 간식이 사라진다", "멍이 간식을 지킨다", "냥이 간식이 사라진다"),
        };
    }
}

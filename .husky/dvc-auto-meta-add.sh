#  DVC에 올라간 원본의 .meta 자동 정리	
#  이미 .meta가 DVC에 있는 경우 스킵	
#  Git 추적 중인 .meta 자동 해제	
#  프로젝트 훅 구조 변경 없음	

#!/bin/sh

git ls-files '*.dvc' -z | while IFS= read -r -d '' dvcfile; do
    orig="${dvcfile%.dvc}"          # .dvc 제거 → 원본 경로
    metafile="${orig}.meta"         # .meta 파일
    metadvc="${metafile}.dvc"       # .meta.dvc 파일

    # .meta 자체가 없으면 패스
    [ ! -f "$metafile" ] && continue

    # 이미 meta가 DVC이면 패스
    if [ -f "$metadvc" ]; then
        echo "⏩ SKIP (already tracked): $metafile"
        continue
    fi

    echo "🧩 ADD META → $metafile"
    dvc add "$metafile" >/dev/null 2>&1
    git add "$metadvc"

    # Git이 추적 중이면 해제
    if git ls-files --cached --error-unmatch "$metafile" >/dev/null 2>&1; then
        git rm --cached "$metafile" >/dev/null 2>&1
    fi
done

echo ""
echo "✅ META sync complete. 이제 커밋 + dvc push 해줘."

# 3-Minute Demo Script

1. Start platform: `dotnet run --project src/RadioPulse.AppHost/RadioPulse.AppHost.csproj`
2. Open Web app from Aspire dashboard and navigate:
   - `Media`: show now playing + schedule.
   - `Engagement`: create poll, vote in one tab, show live update in second tab.
3. In `Engagement`, post shoutout and watch real-time stream.
4. Open `Recommendations` page to show ML-driven station ranking.
5. Mention Worker transcript pipeline:
   - open `Media` and show `AI Top Moments` and transcript search.
6. Optional mobile pass:
   - run `dotnet build src/RadioPulse.Mobile/RadioPulse.Mobile.csproj -f net10.0-windows10.0.19041.0`
   - show Login, Now Playing, Vote, Recommendations tabs.

using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace TrackerApp;

public static class Dashboard
{
    public static int Port { get; set; } = 5005;

    public static void Open()
    {
        try
        {
            Process.Start(new ProcessStartInfo($"http://localhost:{Port}/") { UseShellExecute = true });
        }
        catch
        {
            // fallback in case of shell execute errors
        }
    }

    public static string BuildHtml(Database db)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var monday = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek + 1).ToString("yyyy-MM-dd");

        var todayRaw = db.QueryReport(today);
        var weekRaw = db.QueryReport(monday);
        var allRaw = db.QueryReport(null);

        var todayData = todayRaw.Select(x => new { process = x.process, category = x.category, customName = x.customName, seconds = x.totalSeconds }).ToList();
        var weekData = db.QueryDailyTotals(7).Select(x => new { day = x.day, seconds = x.totalSeconds }).ToList();
        var weekApps = weekRaw.Select(x => new { process = x.process, category = x.category, customName = x.customName, seconds = x.totalSeconds }).ToList();
        var allData = allRaw.Select(x => new { process = x.process, category = x.category, customName = x.customName, seconds = x.totalSeconds }).ToList();

        var todayTotal = todayData.Sum(x => x.seconds);
        var weekTotal = weekRaw.Sum(x => x.totalSeconds);
        var grandTotal = db.QueryGrandTotal();

        var allProcessMappings = db.QueryAllProcessMappings().Select(x => new { process = x.process, category = x.category, customName = x.customName }).ToList();

        var todayDataJson = JsonSerializer.Serialize(todayData);
        var weekDataJson = JsonSerializer.Serialize(weekData);
        var weekAppsJson = JsonSerializer.Serialize(weekApps);
        var allDataJson = JsonSerializer.Serialize(allData);
        var allProcessMappingsJson = JsonSerializer.Serialize(allProcessMappings);

        var html = GetTemplate();

        return html
            .Replace("{{TODAY_DATE}}", today)
            .Replace("{{TODAY_DATA}}", todayDataJson)
            .Replace("{{WEEK_DATA}}", weekDataJson)
            .Replace("{{WEEK_APPS}}", weekAppsJson)
            .Replace("{{ALL_DATA}}", allDataJson)
            .Replace("{{PROCESS_MAPPINGS}}", allProcessMappingsJson)
            .Replace("{{TODAY_TOTAL}}", todayTotal.ToString())
            .Replace("{{WEEK_TOTAL}}", weekTotal.ToString())
            .Replace("{{GRAND_TOTAL}}", grandTotal.ToString());
    }

    private static string GetTemplate()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>Activity Tracker - Dashboard</title>
<link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500;600&display=swap"" rel=""stylesheet"">
<style>
  :root {
    --bg: #09090b;
    --surface: rgba(24, 24, 27, 0.75);
    --surface-hover: rgba(39, 39, 42, 0.85);
    --border: rgba(255, 255, 255, 0.08);
    --border-hover: rgba(255, 255, 255, 0.16);
    --border-focus: rgba(99, 102, 241, 0.4);
    --text: #a1a1aa;
    --heading: #fafafa;
    --muted: #52525b;
    --primary: #6366f1;
    --primary-glow: rgba(99, 102, 241, 0.15);
    --accent: #a5b4fc;
    --card-shadow: 0 4px 30px rgba(0, 0, 0, 0.4);
  }

  * { margin:0; padding:0; box-sizing:border-box; }
  body {
    font-family: 'Outfit', sans-serif;
    background: var(--bg);
    color: var(--text);
    padding: 32px 40px;
    min-height: 100vh;
    overflow-y: scroll;
    background-image: 
      radial-gradient(at 0% 0%, rgba(99, 102, 241, 0.08) 0px, transparent 50%),
      radial-gradient(at 100% 100%, rgba(139, 92, 246, 0.05) 0px, transparent 50%);
  }

  .container {
    max-width: 1200px;
    margin: 0 auto;
  }

  /* Header & Navigation */
  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 28px;
  }
  .brand-group {
    display: flex;
    align-items: baseline;
    gap: 12px;
  }
  .brand-group h1 {
    color: var(--heading);
    font-size: 28px;
    font-weight: 700;
    letter-spacing: -0.5px;
    background: linear-gradient(135deg, #fafafa 0%, #a5b4fc 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .brand-group span {
    color: var(--muted);
    font-size: 14px;
    font-family: 'JetBrains Mono', monospace;
  }

  .nav-tabs {
    display: flex;
    gap: 6px;
    background: rgba(255, 255, 255, 0.03);
    padding: 4px;
    border-radius: 8px;
    border: 1px solid var(--border);
  }
  .tab-btn {
    background: transparent;
    border: none;
    color: var(--text);
    padding: 8px 16px;
    font-size: 14px;
    font-weight: 500;
    cursor: pointer;
    border-radius: 6px;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  }
  .tab-btn:hover {
    color: var(--heading);
  }
  .tab-btn.active {
    color: var(--heading);
    background: rgba(255, 255, 255, 0.08);
    box-shadow: 0 2px 8px rgba(0,0,0,0.2);
  }

  /* KPI Grid */
  .kpi-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 16px;
    margin-bottom: 24px;
  }
  .kpi {
    background: var(--surface);
    border: 1px solid var(--border);
    backdrop-filter: blur(16px);
    border-radius: 12px;
    padding: 20px 24px;
    display: flex;
    flex-direction: column;
    gap: 6px;
    box-shadow: var(--card-shadow);
    transition: border-color 0.3s;
  }
  .kpi:hover {
    border-color: var(--border-hover);
  }
  .kpi-label {
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: 1px;
    color: var(--muted);
    font-weight: 600;
  }
  .kpi-value {
    font-size: 32px;
    font-weight: 700;
    color: var(--heading);
    font-family: 'JetBrains Mono', monospace;
  }
  .kpi-sub {
    font-size: 13px;
    color: var(--muted);
  }

  /* Main Grid Layout */
  .grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px;
  }
  .card {
    background: var(--surface);
    border: 1px solid var(--border);
    backdrop-filter: blur(16px);
    border-radius: 12px;
    padding: 24px;
    box-shadow: var(--card-shadow);
    display: flex;
    flex-direction: column;
    min-height: 360px;
  }
  .card.full {
    grid-column: 1 / -1;
  }
  .card h2 {
    color: var(--heading);
    font-size: 18px;
    font-weight: 600;
    margin-bottom: 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  .card h2 span {
    font-size: 12px;
    color: var(--muted);
    font-family: 'JetBrains Mono', monospace;
  }

  /* Donut Layout */
  .donut-wrap {
    display: flex;
    align-items: center;
    gap: 32px;
    flex: 1;
  }
  .donut-svg-container {
    position: relative;
    width: 140px;
    height: 140px;
    flex-shrink: 0;
  }
  .donut {
    width: 100%;
    height: 100%;
  }
  .donut path {
    transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.3s;
    cursor: pointer;
    transform-origin: 48px 48px;
  }
  .donut path:hover, .donut path.hovered {
    transform: scale(1.05);
    opacity: 1 !important;
  }

  .legend {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 10px;
    max-height: 250px;
    overflow-y: auto;
    padding-right: 4px;
  }
  .legend-item {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 14px;
    padding: 6px 10px;
    border-radius: 6px;
    cursor: pointer;
    transition: background-color 0.2s, transform 0.15s;
    border: 1px solid transparent;
  }
  .legend-item:hover, .legend-item.active {
    background: rgba(255, 255, 255, 0.04);
    transform: translateX(4px);
    border-color: rgba(255,255,255,0.03);
  }
  .legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
  }
  .legend-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-weight: 500;
  }
  .legend-time {
    color: var(--heading);
    font-family: 'JetBrains Mono', monospace;
    font-size: 13px;
  }

  /* Detail Breakdown Section */
  .details-box {
    margin-top: 20px;
    border-top: 1px solid var(--border);
    padding-top: 16px;
    display: none;
    animation: fadeIn 0.3s ease;
  }
  .details-title {
    font-size: 13px;
    font-weight: 600;
    color: var(--heading);
    margin-bottom: 12px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    display: flex;
    justify-content: space-between;
  }
  .details-grid {
    display: flex;
    flex-direction: column;
    gap: 8px;
    max-height: 200px;
    overflow-y: auto;
    padding-right: 4px;
  }
  .detail-row {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .detail-name {
    width: 160px;
    font-size: 13px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: var(--text);
  }
  .detail-bar-track {
    flex: 1;
    height: 8px;
    background: rgba(255, 255, 255, 0.05);
    border-radius: 4px;
    overflow: hidden;
  }
  .detail-bar-fill {
    height: 100%;
    border-radius: 4px;
    transition: width 0.5s ease;
  }
  .detail-time {
    width: 70px;
    text-align: right;
    font-size: 12px;
    font-family: 'JetBrains Mono', monospace;
    color: var(--heading);
  }

  /* Weekly column chart */
  .week-wrap {
    display: flex;
    align-items: flex-end;
    gap: 16px;
    height: 220px;
    padding-top: 12px;
    flex: 1;
  }
  .week-col {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    height: 100%;
    justify-content: flex-end;
  }
  .week-bar-container {
    width: 100%;
    max-width: 50px;
    height: 100%;
    display: flex;
    align-items: flex-end;
    position: relative;
  }
  .week-bar {
    width: 100%;
    border-radius: 6px 6px 0 0;
    background: linear-gradient(to top, var(--primary), var(--accent));
    transition: height 0.6s cubic-bezier(0.4, 0, 0.2, 1);
    min-height: 4px;
    box-shadow: 0 4px 12px rgba(99, 102, 241, 0.2);
  }
  .week-val {
    font-size: 12px;
    color: var(--muted);
    font-family: 'JetBrains Mono', monospace;
    margin-bottom: 4px;
  }
  .week-day {
    font-size: 12px;
    color: var(--text);
    text-transform: uppercase;
    font-weight: 600;
  }
  .week-today .week-bar {
    background: linear-gradient(to top, #818cf8, #c7d2fe);
    box-shadow: 0 4px 15px rgba(129, 140, 248, 0.35);
  }
  .week-today .week-day {
    color: #a5b4fc;
    font-weight: 700;
  }

  /* Category breakdown cards */
  .cat-breakdown-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 16px;
    margin-top: 8px;
  }
  .cat-card {
    background: rgba(255,255,255,0.02);
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 16px;
    transition: transform 0.2s, border-color 0.2s;
  }
  .cat-card:hover {
    transform: translateY(-2px);
    border-color: var(--border-hover);
  }
  .cat-card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
  }
  .cat-card-title {
    font-size: 15px;
    font-weight: 600;
    color: var(--heading);
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .cat-card-time {
    font-size: 14px;
    font-family: 'JetBrains Mono', monospace;
    font-weight: 600;
    color: var(--primary);
  }

  /* Settings Page styles */
  .view-pane {
    display: none;
  }
  .view-pane.active {
    display: block;
    animation: fadeIn 0.4s ease;
  }

  .settings-controls {
    display: flex;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 20px;
  }
  .search-box {
    position: relative;
    flex: 1;
    max-width: 400px;
  }
  .search-box input {
    width: 100%;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid var(--border);
    color: var(--heading);
    padding: 10px 16px 10px 40px;
    border-radius: 8px;
    font-family: inherit;
    font-size: 14px;
    outline: none;
    transition: all 0.2s;
  }
  .search-box input:focus {
    border-color: var(--primary);
    background: rgba(255, 255, 255, 0.05);
    box-shadow: 0 0 10px var(--primary-glow);
  }
  .search-box svg {
    position: absolute;
    left: 14px;
    top: 50%;
    transform: translateY(-50%);
    width: 16px;
    height: 16px;
    fill: var(--muted);
  }

  .table-container {
    border: 1px solid var(--border);
    border-radius: 8px;
    overflow: hidden;
    background: var(--surface);
    box-shadow: var(--card-shadow);
  }
  .mapping-table {
    width: 100%;
    border-collapse: collapse;
    text-align: left;
    font-size: 14px;
  }
  .mapping-table th, .mapping-table td {
    padding: 12px 20px;
    border-bottom: 1px solid var(--border);
  }
  .mapping-table th {
    background: rgba(255, 255, 255, 0.02);
    color: var(--heading);
    font-weight: 600;
  }
  .mapping-table tr:last-child td {
    border-bottom: none;
  }
  .mapping-table tr:hover td {
    background: rgba(255, 255, 255, 0.01);
  }
  .proc-badge {
    font-family: 'JetBrains Mono', monospace;
    font-size: 13px;
    color: var(--heading);
    background: rgba(255, 255, 255, 0.05);
    padding: 4px 8px;
    border-radius: 4px;
    border: 1px solid var(--border);
  }

  .select-cat {
    background: #141416;
    border: 1px solid var(--border);
    color: var(--heading);
    padding: 6px 12px;
    border-radius: 6px;
    outline: none;
    font-family: inherit;
    font-size: 13px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .select-cat:focus {
    border-color: var(--primary);
  }

  .custom-cat-group {
    display: flex;
    gap: 8px;
    align-items: center;
  }
  .custom-cat-input {
    background: #141416;
    border: 1px solid var(--border);
    color: var(--heading);
    padding: 6px 12px;
    border-radius: 6px;
    outline: none;
    font-family: inherit;
    font-size: 13px;
    width: 140px;
    transition: all 0.2s;
  }
  .custom-cat-input:focus {
    border-color: var(--primary);
  }
  
  .btn-save {
    background: var(--primary);
    color: white;
    border: none;
    padding: 6px 12px;
    font-size: 12px;
    font-weight: 600;
    border-radius: 6px;
    cursor: pointer;
    transition: background-color 0.2s;
  }
  .btn-save:hover {
    background: #4f46e5;
  }

  /* Empty state */
  .empty {
    color: var(--muted);
    font-style: italic;
    padding: 40px;
    text-align: center;
    font-size: 15px;
    grid-column: 1/-1;
  }

  /* Toast Notification */
  .toast {
    position: fixed;
    bottom: 24px;
    right: 24px;
    background: #10b981;
    color: white;
    padding: 12px 24px;
    border-radius: 8px;
    box-shadow: 0 4px 15px rgba(16, 185, 129, 0.3);
    font-size: 14px;
    font-weight: 500;
    transform: translateY(150%);
    transition: transform 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
    z-index: 1000;
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .toast.show {
    transform: translateY(0);
  }

  /* Scrollbar custom styling */
  ::-webkit-scrollbar {
    width: 6px;
    height: 6px;
  }
  ::-webkit-scrollbar-track {
    background: transparent;
  }
  ::-webkit-scrollbar-thumb {
    background: rgba(255, 255, 255, 0.1);
    border-radius: 10px;
  }
  ::-webkit-scrollbar-thumb:hover {
    background: rgba(255, 255, 255, 0.2);
  }

  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(6px); }
    to { opacity: 1; transform: translateY(0); }
  }

  /* Timeline View Styles */
  .timeline-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    background: var(--surface);
    padding: 16px 20px;
    border-radius: 10px;
    border: 1px solid var(--border);
  }
  .timeline-nav-group {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .timeline-btn {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border);
    color: var(--heading);
    padding: 8px 14px;
    border-radius: 6px;
    font-size: 13px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;
  }
  .timeline-btn:hover {
    background: rgba(255, 255, 255, 0.1);
    border-color: var(--border-hover);
  }
  .timeline-btn.active {
    background: var(--primary-glow);
    border-color: var(--border-focus);
  }
  .timeline-title {
    font-size: 16px;
    font-weight: 600;
    color: var(--heading);
    font-family: 'JetBrains Mono', monospace;
  }
  
  .timeline-container {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }
  
  /* Timeline entries list */
  .timeline-list {
    position: relative;
    padding-left: 20px;
    border-left: 2px solid rgba(255, 255, 255, 0.05);
    margin-left: 10px;
  }
  .timeline-entry-node {
    position: relative;
    margin-bottom: 20px;
  }
  .timeline-entry-node::before {
    content: '';
    position: absolute;
    left: -26px;
    top: 6px;
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: var(--primary);
    box-shadow: 0 0 8px var(--primary);
    border: 2px solid var(--bg);
  }
  .timeline-entry-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 14px 18px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .timeline-entry-card:hover {
    border-color: var(--border-hover);
    background: var(--surface-hover);
  }
  .timeline-entry-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  .timeline-entry-title-group {
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .timeline-entry-title {
    font-size: 14px;
    font-weight: 600;
    color: var(--heading);
  }
  .timeline-entry-process {
    font-size: 11px;
    color: var(--muted);
    font-family: 'JetBrains Mono', monospace;
  }
  .timeline-entry-meta {
    display: flex;
    align-items: center;
    gap: 12px;
    font-size: 13px;
  }
  .timeline-entry-time {
    font-family: 'JetBrains Mono', monospace;
    color: var(--text);
  }
  .timeline-entry-duration {
    font-weight: 600;
    color: var(--primary);
    font-family: 'JetBrains Mono', monospace;
  }
  .timeline-entry-badge {
    font-size: 11px;
    padding: 2px 6px;
    border-radius: 4px;
    font-weight: 500;
  }
  
  /* Expanded title changes list */
  .timeline-entry-details {
    display: none;
    border-top: 1px solid var(--border);
    padding-top: 10px;
    margin-top: 4px;
    flex-direction: column;
    gap: 6px;
    animation: fadeIn 0.2s ease;
  }
  .timeline-entry-details.open {
    display: flex;
  }
  .timeline-sub-entry {
    display: flex;
    justify-content: space-between;
    font-size: 12px;
    color: var(--text);
    padding-left: 8px;
    border-left: 2px solid var(--border);
  }
  .timeline-sub-title {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 500px;
  }
  .timeline-sub-time {
    font-family: 'JetBrains Mono', monospace;
    color: var(--muted);
    flex-shrink: 0;
  }
  
  /* Weekly Timeline Day Accordions */
  .week-day-accordion {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 8px;
    overflow: hidden;
    margin-bottom: 12px;
  }
  .week-day-header {
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
    font-weight: 600;
    transition: background-color 0.2s;
  }
  .week-day-header:hover {
    background: rgba(255, 255, 255, 0.02);
  }
  .week-day-title-group {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .week-day-name {
    font-size: 15px;
    color: var(--heading);
  }
  .week-day-date {
    font-size: 12px;
    color: var(--muted);
    font-family: 'JetBrains Mono', monospace;
  }
  .week-day-total {
    font-family: 'JetBrains Mono', monospace;
    color: var(--primary);
  }
  .week-day-content {
    display: none;
    padding: 20px;
    border-top: 1px solid var(--border);
    background: rgba(0, 0, 0, 0.15);
  }
  .week-day-content.open {
    display: block;
  }
  .week-day-header-right {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .week-day-chevron {
    transition: transform 0.2s ease, color 0.2s ease;
    color: var(--muted);
    flex-shrink: 0;
  }
  .week-day-accordion.open .week-day-chevron {
    transform: rotate(180deg);
    color: var(--primary);
  }
</style>
</head>
<body>
<div class=""container"">
  
  <div class=""header"">
    <div class=""brand-group"">
      <h1>Activity Tracker</h1>
      <span>{{TODAY_DATE}}</span>
    </div>
    <div class=""nav-tabs"">
      <button class=""tab-btn active"" onclick=""switchTab('dashboard')"">Dashboard</button>
      <button class=""tab-btn"" onclick=""switchTab('timeline')"">Timeline</button>
      <button class=""tab-btn"" onclick=""switchTab('settings')"">Categories Mapping</button>
    </div>
  </div>

  <!-- VIEW: DASHBOARD -->
  <div id=""view-dashboard"" class=""view-pane active"">
    <div class=""kpi-grid"">
      <div class=""kpi"">
        <span class=""kpi-label"">Today</span>
        <span class=""kpi-value"" id=""kpi-today"">0s</span>
        <span class=""kpi-sub"" id=""kpi-today-sub"">0 categories</span>
      </div>
      <div class=""kpi"">
        <span class=""kpi-label"">This Week</span>
        <span class=""kpi-value"" id=""kpi-week"">0s</span>
        <span class=""kpi-sub"" id=""kpi-week-sub"">0 categories</span>
      </div>
      <div class=""kpi"">
        <span class=""kpi-label"">All Time</span>
        <span class=""kpi-value"" id=""kpi-all"">0s</span>
        <span class=""kpi-sub"" id=""kpi-all-sub"">0 categories</span>
      </div>
    </div>

    <div class=""grid"">
      <!-- Today Card -->
      <div class=""card"">
        <h2>Today <span>Grouped by Category</span></h2>
        <div class=""donut-wrap"" id=""today-donut-wrap"">
          <div class=""donut-svg-container"">
            <svg class=""donut"" id=""today-donut"" viewBox=""0 0 96 96""></svg>
          </div>
          <div class=""legend"" id=""today-legend""></div>
        </div>
        
        <!-- Category details box -->
        <div class=""details-box"" id=""today-details"">
          <div class=""details-title"" id=""today-details-title""></div>
          <div class=""details-grid"" id=""today-details-grid""></div>
        </div>
      </div>

      <!-- Week Card -->
      <div class=""card"">
        <h2>This Week <span>Grouped by Category</span></h2>
        <div class=""donut-wrap"" id=""week-donut-wrap"">
          <div class=""donut-svg-container"">
            <svg class=""donut"" id=""week-donut"" viewBox=""0 0 96 96""></svg>
          </div>
          <div class=""legend"" id=""week-legend""></div>
        </div>
        
        <!-- Category details box -->
        <div class=""details-box"" id=""week-details"">
          <div class=""details-title"" id=""week-details-title""></div>
          <div class=""details-grid"" id=""week-details-grid""></div>
        </div>
      </div>

      <!-- Last 7 days column chart -->
      <div class=""card full"">
        <h2>Last 7 Days <span>Daily tracking volume</span></h2>
        <div class=""week-wrap"" id=""week-bars""></div>
      </div>

      <!-- All time full list grouped by categories -->
      <div class=""card full"">
        <h2>All Time Categories Breakdown</h2>
        <div class=""cat-breakdown-grid"" id=""all-time-cats""></div>
      </div>
    </div>
  </div>

  <!-- VIEW: TIMELINE -->
  <div id=""view-timeline"" class=""view-pane"">
    <div class=""timeline-header"">
      <div class=""timeline-nav-group"">
        <button class=""timeline-btn active"" id=""timeline-mode-day"" onclick=""setTimelineMode('day')"">Day</button>
        <button class=""timeline-btn"" id=""timeline-mode-week"" onclick=""setTimelineMode('week')"">Week</button>
      </div>
      <div class=""timeline-title"" id=""timeline-date-label""></div>
      <div class=""timeline-nav-group"">
        <button class=""timeline-btn"" onclick=""navigateTimeline(-1)"">&larr; Prev</button>
        <button class=""timeline-btn"" id=""timeline-next-btn"" onclick=""navigateTimeline(1)"">Next &rarr;</button>
      </div>
    </div>
    
    <div class=""timeline-container"" id=""timeline-content-area"">
      <!-- Renders dynamically -->
    </div>
  </div>

  <!-- VIEW: SETTINGS -->
  <div id=""view-settings"" class=""view-pane"">
    <div class=""card full"">
      <h2>Manage Categories Mappings</h2>
      
      <div class=""settings-controls"">
        <div class=""search-box"">
          <svg viewBox=""0 0 24 24"">
            <path d=""M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z""/>
          </svg>
          <input type=""text"" id=""search-proc"" placeholder=""Search process name (e.g. chrome)..."" oninput=""filterMappings()"">
        </div>
      </div>

      <div class=""table-container"">
        <table class=""mapping-table"">
          <thead>
            <tr>
              <th>Process Name</th>
              <th>Display Name</th>
              <th>Category</th>
              <th>Custom Category</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody id=""mapping-table-body""></tbody>
        </table>
      </div>
    </div>
  </div>

</div>

<!-- Floating Toast -->
<div class=""toast"" id=""toast"">
  <svg width=""16"" height=""16"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor"" stroke-width=""2"">
    <path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M5 13l4 4L19 7"" />
  </svg>
  <span id=""toast-msg"">Mapping updated!</span>
</div>

<script>
  // Data injected by WebServer
  const rawToday = {{TODAY_DATA}};
  const rawWeekApps = {{WEEK_APPS}};
  const rawAllTime = {{ALL_DATA}};
  const rawWeekDaily = {{WEEK_DATA}};
  
  const todayTotal = {{TODAY_TOTAL}};
  const weekTotal = {{WEEK_TOTAL}};
  const grandTotal = {{GRAND_TOTAL}};
  
  const processMappings = {{PROCESS_MAPPINGS}};

  // Curated color palette
  const colors = [
    ""#6366f1"", ""#10b981"", ""#f59e0b"", ""#ef4444"", ""#8b5cf6"", ""#06b6d4"",
    ""#ec4899"", ""#14b8a6"", ""#f97316"", ""#3b82f6"", ""#84cc16"", ""#64748b""
  ];

  const standardCategories = [
    ""Development"", ""Games"", ""Browsing"", ""Communication"", ""Media"", ""Work"", ""System"", ""Ignored"", ""Other""
  ];

  // Helper: Format seconds to readable duration
  function formatDuration(s) {
    const h = Math.floor(s / 3600);
    const m = Math.floor((s % 3600) / 60);
    const sec = s % 60;
    if (h > 0) return `${h}h ${m}m`;
    if (m > 0) return `${m}m`;
    return `${sec}s`;
  }

  // Switch tabs
  function switchTab(tabId) {
    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
    document.querySelectorAll('.view-pane').forEach(pane => pane.classList.remove('active'));

    const btn = Array.from(document.querySelectorAll('.tab-btn')).find(b => b.innerText.toLowerCase().includes(tabId.substring(0, 4)));
    if (btn) btn.classList.add('active');

    const view = document.getElementById('view-' + tabId);
    if (view) view.classList.add('active');
  }

  // Group processes by category
  function aggregateByCategory(rawData) {
    const map = {};
    rawData.forEach(item => {
      const cat = item.category || 'Uncategorized';
      if (!map[cat]) {
        map[cat] = { seconds: 0, items: [] };
      }
      map[cat].seconds += item.seconds;
      map[cat].items.push(item);
    });
    return Object.entries(map)
      .map(([name, data]) => ({ name, seconds: data.seconds, items: data.items.sort((a,b) => b.seconds - a.seconds) }))
      .sort((a, b) => b.seconds - a.seconds);
  }

  // Draw dynamic SVG Donut Chart
  function drawDonut(svgId, legendId, detailBoxId, data, totalSeconds) {
    const svg = document.getElementById(svgId);
    const legend = document.getElementById(legendId);
    const detailBox = document.getElementById(detailBoxId);
    
    svg.innerHTML = '';
    legend.innerHTML = '';
    detailBox.style.display = 'none';

    if (data.length === 0 || totalSeconds === 0) {
      document.getElementById(svgId + '-wrap').innerHTML = '<div class=""empty"">No data yet</div>';
      return;
    }

    const aggregated = aggregateByCategory(data);

    // Render slices
    let angle = -90;
    const slices = [];
    
    aggregated.forEach((cat, idx) => {
      const pct = cat.seconds / totalSeconds;
      const sweep = pct * 360;
      const color = colors[idx % colors.length];
      cat.color = color; // Save for legend reference
      cat.pct = pct;

      if (pct >= 0.999) {
        // Special case: Single category dominates
        const circle = document.createElementNS(""http://www.w3.org/2000/svg"", ""circle"");
        circle.setAttribute(""cx"", ""48"");
        circle.setAttribute(""cy"", ""48"");
        circle.setAttribute(""r"", ""44"");
        circle.setAttribute(""fill"", color);
        circle.setAttribute(""opacity"", ""0.85"");
        circle.addEventListener('mouseenter', () => highlightSlice(idx, svgId, legendId));
        circle.addEventListener('mouseleave', () => clearHighlights(svgId, legendId));
        circle.addEventListener('click', () => showCategoryDetail(cat, color, detailBoxId));
        svg.appendChild(circle);
      } else if (sweep > 0) {
        const radStart = (angle * Math.PI) / 180;
        const startX = 48 + 44 * Math.cos(radStart);
        const startY = 48 + 44 * Math.sin(radStart);

        angle += sweep;
        const radEnd = (angle * Math.PI) / 180;
        const endX = 48 + 44 * Math.cos(radEnd);
        const endY = 48 + 44 * Math.sin(radEnd);

        const largeArc = sweep > 180 ? 1 : 0;

        // Path: Move to center, draw line to start, draw arc to end, close path
        const pathData = `M 48 48 L ${startX.toFixed(1)} ${startY.toFixed(1)} A 44 44 0 ${largeArc} 1 ${endX.toFixed(1)} ${endY.toFixed(1)} Z`;
        
        const path = document.createElementNS(""http://www.w3.org/2000/svg"", ""path"");
        path.setAttribute(""d"", pathData);
        path.setAttribute(""fill"", color);
        path.setAttribute(""opacity"", ""0.85"");
        path.setAttribute(""data-idx"", idx);
        
        path.addEventListener('mouseenter', () => highlightSlice(idx, svgId, legendId));
        path.addEventListener('mouseleave', () => clearHighlights(svgId, legendId));
        path.addEventListener('click', () => showCategoryDetail(cat, color, detailBoxId));

        svg.appendChild(path);
      }
    });

    // Donut hole mask (always drawn after slices to sit on top!)
    const mask = document.createElementNS(""http://www.w3.org/2000/svg"", ""circle"");
    mask.setAttribute(""cx"", ""48"");
    mask.setAttribute(""cy"", ""48"");
    mask.setAttribute(""r"", ""24"");
    mask.setAttribute(""fill"", ""#09090b""); // Matches background color
    svg.appendChild(mask);

    // Render legend
    aggregated.forEach((cat, idx) => {
      const pctStr = (cat.pct * 100).toFixed(1) + '%';
      
      const item = document.createElement('div');
      item.className = 'legend-item';
      item.setAttribute('data-idx', idx);
      item.innerHTML = `
        <span class=""legend-dot"" style=""background:${cat.color}""></span>
        <span class=""legend-name"" title=""${cat.name} (${pctStr})"">${cat.name}</span>
        <span class=""legend-time"">${formatDuration(cat.seconds)}</span>
      `;

      item.addEventListener('mouseenter', () => highlightSlice(idx, svgId, legendId));
      item.addEventListener('mouseleave', () => clearHighlights(svgId, legendId));
      item.addEventListener('click', () => showCategoryDetail(cat, cat.color, detailBoxId));

      legend.appendChild(item);
    });

    // Auto-select first category as default detail view
    if (aggregated.length > 0) {
      showCategoryDetail(aggregated[0], aggregated[0].color, detailBoxId);
      // Add active state to the first legend item
      setTimeout(() => {
        const firstLeg = legend.querySelector(`.legend-item[data-idx=""0""]`);
        if (firstLeg) firstLeg.classList.add('active');
      }, 50);
    }
  }

  // Hover animations for donut slice
  function highlightSlice(idx, svgId, legendId) {
    const svg = document.getElementById(svgId);
    const legend = document.getElementById(legendId);

    // Fade out other slices, scale target
    svg.querySelectorAll('path, circle').forEach(el => {
      if (el.tagName === 'circle' && el.getAttribute('r') === '24') return; // Skip mask
      const sliceIdx = el.getAttribute('data-idx');
      if (sliceIdx == idx) {
        el.classList.add('hovered');
      } else {
        el.style.opacity = '0.3';
      }
    });

    // Highlight legend item
    legend.querySelectorAll('.legend-item').forEach(item => {
      const legIdx = item.getAttribute('data-idx');
      if (legIdx == idx) {
        item.classList.add('active');
      } else {
        item.classList.remove('active');
      }
    });
  }

  function clearHighlights(svgId, legendId) {
    const svg = document.getElementById(svgId);
    const legend = document.getElementById(legendId);

    svg.querySelectorAll('path, circle').forEach(el => {
      el.classList.remove('hovered');
      el.style.opacity = '0.85';
    });

    legend.querySelectorAll('.legend-item').forEach(item => {
      item.classList.remove('active');
    });
  }

  // Category process details expand
  function showCategoryDetail(categoryData, color, detailBoxId) {
    const detailBox = document.getElementById(detailBoxId);
    const title = document.getElementById(detailBoxId + '-title');
    const grid = document.getElementById(detailBoxId + '-grid');

    title.innerHTML = `<span>${categoryData.name} Breakdown</span> <span>${formatDuration(categoryData.seconds)}</span>`;
    grid.innerHTML = '';

    categoryData.items.forEach(proc => {
      const pct = (proc.seconds / categoryData.seconds) * 100;
      const displayName = proc.customName || proc.process;
      const row = document.createElement('div');
      row.className = 'detail-row';
      row.innerHTML = `
        <span class=""detail-name"" title=""${proc.process}"">${displayName}</span>
        <div class=""detail-bar-track"">
          <div class=""detail-bar-fill"" style=""width:${pct.toFixed(0)}%; background:${color}""></div>
        </div>
        <span class=""detail-time"">${formatDuration(proc.seconds)}</span>
      `;
      grid.appendChild(row);
    });

    detailBox.style.display = 'block';
  }

  // Draw 7-day Column Bar Chart
  function drawWeekChart() {
    const container = document.getElementById('week-bars');
    container.innerHTML = '';
    
    if (rawWeekDaily.length === 0) {
      container.innerHTML = '<div class=""empty"">No data yet</div>';
      return;
    }

    const maxSeconds = Math.max(...rawWeekDaily.map(x => x.seconds));
    
    // Fill the last 7 days (including empty ones)
    const todayStr = '{{TODAY_DATE}}';
    
    for (let i = 6; i >= 0; i--) {
      const d = new Date();
      d.setDate(d.getDate() - i);
      const yyyy = d.getFullYear();
      const mm = String(d.getMonth() + 1).padStart(2, '0');
      const dd = String(d.getDate()).padStart(2, '0');
      const dateStr = `${yyyy}-${mm}-${dd}`;
      const dayName = d.toLocaleDateString('en-US', { weekday: 'short' });

      const dayData = rawWeekDaily.find(x => x.day === dateStr);
      const sec = dayData ? dayData.seconds : 0;
      const heightPct = maxSeconds > 0 ? (sec / maxSeconds) * 90 : 0; // max height is 90%

      const col = document.createElement('div');
      col.className = dateStr === todayStr ? 'week-col week-today' : 'week-col';
      col.innerHTML = `
        <span class=""week-val"">${sec > 0 ? formatDuration(sec) : ''}</span>
        <div class=""week-bar-container"">
          <div class=""week-bar"" style=""height:${heightPct.toFixed(0)}%"" title=""${dateStr}: ${formatDuration(sec)}""></div>
        </div>
        <span class=""week-day"">${dayName}</span>
      `;
      container.appendChild(col);
    }
  }

  // Render All Time breakdowns card list
  function drawAllTimeBreakdown() {
    const grid = document.getElementById('all-time-cats');
    grid.innerHTML = '';

    const aggregated = aggregateByCategory(rawAllTime);

    if (aggregated.length === 0) {
      grid.innerHTML = '<div class=""empty"">No data yet</div>';
      return;
    }

    aggregated.forEach((cat, idx) => {
      const card = document.createElement('div');
      card.className = 'cat-card';

      const color = colors[idx % colors.length];

      let procRows = '';
      cat.items.slice(0, 5).forEach(proc => {
        const pct = (proc.seconds / cat.seconds) * 100;
        const displayName = proc.customName || proc.process;
        procRows += `
          <div class=""detail-row"" style=""margin-bottom:6px"">
            <span class=""detail-name"" style=""width:120px; font-size:12px"" title=""${proc.process}"">${displayName}</span>
            <div class=""detail-bar-track"" style=""height:5px"">
              <div class=""detail-bar-fill"" style=""width:${pct.toFixed(0)}%; background:${color}""></div>
            </div>
            <span class=""detail-time"" style=""font-size:11px"">${formatDuration(proc.seconds)}</span>
          </div>
        `;
      });

      if (cat.items.length > 5) {
        procRows += `<div style=""font-size:11px; color:var(--muted); text-align:center; margin-top:8px"">+ ${cat.items.length - 5} more apps</div>`;
      }

      card.innerHTML = `
        <div class=""cat-card-header"">
          <span class=""cat-card-title"">
            <span class=""legend-dot"" style=""background:${color}; width:8px; height:8px""></span>
            ${cat.name}
          </span>
          <span class=""cat-card-time"">${formatDuration(cat.seconds)}</span>
        </div>
        <div>
          ${procRows}
        </div>
      `;
      grid.appendChild(card);
    });
  }

  // Render Mappings Settings Table
  function renderMappingsTable(data) {
    const tbody = document.getElementById('mapping-table-body');
    tbody.innerHTML = '';

    if (data.length === 0) {
      tbody.innerHTML = '<tr><td colspan=""5"" class=""empty"">No processes tracked yet. Run some apps first!</td></tr>';
      return;
    }

    data.forEach(item => {
      const tr = document.createElement('tr');
      tr.setAttribute('data-proc', item.process.toLowerCase());

      // Options for dropdown
      let optionsHtml = '';
      standardCategories.forEach(cat => {
        const selected = item.category === cat ? 'selected' : '';
        optionsHtml += `<option value=""${cat}"" ${selected}>${cat}</option>`;
      });
      
      const isCustom = !standardCategories.includes(item.category) && item.category !== 'Uncategorized';
      const customVal = isCustom ? item.category : '';
      const selectedCustom = isCustom ? 'selected' : '';
      optionsHtml += `<option value=""_custom_"" ${selectedCustom}>Custom...</option>`;

      const displayVal = item.customName || '';

      tr.innerHTML = `
        <td><span class=""proc-badge"">${item.process}</span></td>
        <td>
          <input type=""text"" class=""custom-cat-input"" id=""display-input-${item.process}"" value=""${displayVal}"" placeholder=""Friendly Name (e.g. VS Code)"" style=""width: 200px;"" onchange=""saveMapping('${item.process}')"">
        </td>
        <td>
          <select class=""select-cat"" onchange=""onCategorySelect(this, '${item.process}')"">
            ${optionsHtml}
          </select>
        </td>
        <td>
          <div class=""custom-cat-group"" id=""custom-group-${item.process}"" style=""display: ${isCustom ? 'flex' : 'none'}"">
            <input type=""text"" class=""custom-cat-input"" id=""custom-input-${item.process}"" value=""${customVal}"" placeholder=""E.g. Gaming"" onchange=""saveMapping('${item.process}')"">
          </div>
        </td>
        <td>
          <button class=""btn-save"" onclick=""saveMapping('${item.process}')"">Save</button>
        </td>
      `;

      tbody.appendChild(tr);
    });
  }

  // Handle category dropdown change
  function onCategorySelect(selectEl, process) {
    const customGroup = document.getElementById(`custom-group-${process}`);
    const customInput = document.getElementById(`custom-input-${process}`);

    if (selectEl.value === '_custom_') {
      customGroup.style.display = 'flex';
      customInput.focus();
    } else {
      customGroup.style.display = 'none';
      // Automatically save if it is a standard category
      saveMapping(process);
    }
  }

  // Save process mapping using AJAX POST
  async function saveMapping(process) {
    const select = document.querySelector(`tr[data-proc=""${process.toLowerCase()}""] .select-cat`);
    const customInput = document.getElementById(`custom-input-${process}`);
    const displayInput = document.getElementById(`display-input-${process}`);
    
    let category = select.value;
    if (category === '_custom_') {
      category = customInput.value.trim();
      if (!category) {
        alert('Please enter a custom category name!');
        customInput.focus();
        return;
      }
    }

    const customName = displayInput.value.trim();

    try {
      const response = await fetch('/api/map', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ process, category, customName })
      });

      if (response.ok) {
        // Update local data
        const item = processMappings.find(x => x.process === process);
        if (item) {
          item.category = category;
          item.customName = customName;
        }

        // Show toast
        showToast(`Saved: ${process} &rarr; ${customName || process} [${category}]`);

        // Update dashboard datasets
        updateLocalDataset(process, category, customName);

        // Re-render dashboard elements
        drawDonut('today-donut', 'today-legend', 'today-details', rawToday, todayTotal);
        drawDonut('week-donut', 'week-legend', 'week-details', rawWeekApps, weekTotal);
        drawAllTimeBreakdown();
      } else {
        alert('Failed to save mapping to server.');
      }
    } catch (e) {
      alert('Error connecting to local server.');
    }
  }

  // Dynamically update categories in memory lists when mapping is updated
  function updateLocalDataset(process, category, customName) {
    const updateList = (list) => {
      list.forEach(item => {
        if (item.process === process) {
          item.category = category;
          item.customName = customName;
        }
      });
    };
    updateList(rawToday);
    updateList(rawWeekApps);
    updateList(rawAllTime);
  }

  // Search filter mappings table
  function filterMappings() {
    const query = document.getElementById('search-proc').value.toLowerCase().trim();
    const rows = document.querySelectorAll('#mapping-table-body tr');

    rows.forEach(row => {
      const proc = row.getAttribute('data-proc');
      if (proc.includes(query)) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  // Display floating toast alert
  function showToast(msg) {
    const toast = document.getElementById('toast');
    const toastMsg = document.getElementById('toast-msg');
    toastMsg.innerHTML = msg;
    
    toast.classList.add('show');
    
    clearTimeout(window.toastTimer);
    window.toastTimer = setTimeout(() => {
      toast.classList.remove('show');
    }, 2500);
  }

  // Timeline UI Variables & Functions
  let activeTimelineMode = 'day';
  let activeTimelineDate = new Date();

  function setTimelineMode(mode) {
    activeTimelineMode = mode;
    document.getElementById('timeline-mode-day').classList.toggle('active', mode === 'day');
    document.getElementById('timeline-mode-week').classList.toggle('active', mode === 'week');
    loadTimelineData();
  }

  function navigateTimeline(offset) {
    if (activeTimelineMode === 'day') {
      activeTimelineDate.setDate(activeTimelineDate.getDate() + offset);
    } else {
      activeTimelineDate.setDate(activeTimelineDate.getDate() + (offset * 7));
    }
    loadTimelineData();
  }

  function formatDateIso(date) {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  async function loadTimelineData() {
    const area = document.getElementById('timeline-content-area');
    area.innerHTML = '<div class=""empty"">Loading...</div>';

    let startStr, endStr;
    if (activeTimelineMode === 'day') {
      startStr = formatDateIso(activeTimelineDate);
      endStr = startStr;
      
      const formattedLabel = activeTimelineDate.toLocaleDateString('en-US', {
        weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
      });
      document.getElementById('timeline-date-label').innerText = formattedLabel;
    } else {
      // Find Monday and Sunday of the active week
      const currentDay = activeTimelineDate.getDay();
      const distanceToMonday = currentDay === 0 ? -6 : 1 - currentDay;
      
      const monday = new Date(activeTimelineDate);
      monday.setDate(monday.getDate() + distanceToMonday);
      
      const sunday = new Date(monday);
      sunday.setDate(sunday.getDate() + 6);
      
      startStr = formatDateIso(monday);
      endStr = sundayStr = formatDateIso(sunday);

      const monLabel = monday.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      const sunLabel = sunday.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
      document.getElementById('timeline-date-label').innerText = `${monLabel} - ${sunLabel}`;
    }

    try {
      const res = await fetch(`/api/timeline?start=${startStr}&end=${endStr}`);
      if (!res.ok) throw new Error('API error');
      const data = await res.json();
      renderTimeline(data);
    } catch (e) {
      area.innerHTML = '<div class=""empty"" style=""color:var(--accent)"">Failed to load history from server</div>';
    }
  }

  function renderTimeline(data) {
    const container = document.getElementById('timeline-content-area');
    container.innerHTML = '';

    if (activeTimelineMode === 'day') {
      renderDailyTimeline(container, data);
    } else {
      renderWeeklyTimeline(container, data);
    }
  }

  function renderDailyTimeline(container, data) {
    if (!data || data.length === 0) {
      container.innerHTML = '<div class=""empty"">No activity tracked for this day</div>';
      return;
    }

    const timelineList = document.createElement('div');
    timelineList.className = 'timeline-list';

    // Group adjacent sessions of the same application
    const groups = [];
    let currentGroup = null;

    data.forEach(session => {
      const appName = session.CustomName || session.Process;
      
      const started = new Date(session.StartedAt);
      const ended = session.EndedAt ? new Date(session.EndedAt) : new Date();

      if (currentGroup && currentGroup.appName === appName && currentGroup.category === session.Category) {
        currentGroup.duration += session.Duration;
        currentGroup.ended = ended;
        currentGroup.sessions.push(session);
      } else {
        if (currentGroup) groups.push(currentGroup);
        currentGroup = {
          appName,
          process: session.Process,
          category: session.Category,
          started,
          ended,
          duration: session.Duration,
          sessions: [session]
        };
      }
    });
    if (currentGroup) groups.push(currentGroup);

    groups.forEach((group, idx) => {
      const node = document.createElement('div');
      node.className = 'timeline-entry-node';
      
      const catIdx = standardCategories.indexOf(group.category);
      const color = catIdx >= 0 ? colors[catIdx % colors.length] : '#a1a1aa';

      const timeStr = `${formatTime(group.started)} - ${formatTime(group.ended)}`;
      const durStr = formatDuration(group.duration);

      let detailsHtml = '';
      group.sessions.forEach(s => {
        const sStart = new Date(s.StartedAt);
        const sEnd = s.EndedAt ? new Date(s.EndedAt) : new Date();
        const sTime = `${formatTime(sStart)} - ${formatTime(sEnd)}`;
        
        const titleText = s.WindowTitle || '(No title)';
        const titleEscaped = titleText
          .replace(/&/g, ""&amp;"")
          .replace(/</g, ""&lt;"")
          .replace(/>/g, ""&gt;"")
          .replace(/""/g, ""&quot;"")
          .replace(/'/g, ""&#039;"");
          
        detailsHtml += `
          <div class=""timeline-sub-entry"">
            <span class=""timeline-sub-title"" title=""${titleEscaped}"">${titleEscaped || '(No title)'}</span>
            <span class=""timeline-sub-time"">${sTime} (${formatDuration(s.Duration)})</span>
          </div>
        `;
      });

      const uniqueIdx = Math.random().toString(36).substr(2, 9);

      node.innerHTML = `
        <div class=""timeline-entry-card"" onclick=""toggleTimelineDetail('${uniqueIdx}')"">
          <div class=""timeline-entry-header"">
            <div class=""timeline-entry-title-group"">
              <span class=""timeline-entry-title"">${group.appName}</span>
              ${group.appName !== group.process ? `<span class=""timeline-entry-process"">${group.process}</span>` : ''}
            </div>
            <span class=""timeline-entry-badge"" style=""background:${color}30; color:${color}; border:1px solid ${color}50"">${group.category}</span>
          </div>
          <div class=""timeline-entry-meta"">
            <span class=""timeline-entry-time"">${timeStr}</span>
            <span class=""timeline-entry-duration"">&bull; ${durStr}</span>
          </div>
          <div class=""timeline-entry-details"" id=""timeline-detail-${uniqueIdx}"">
            ${detailsHtml}
          </div>
        </div>
      `;

      node.style.setProperty('--primary', color);
      timelineList.appendChild(node);
    });

    container.appendChild(timelineList);
  }

  function toggleTimelineDetail(idx) {
    const el = document.getElementById(`timeline-detail-${idx}`);
    if (el) {
      el.classList.toggle('open');
    }
  }

  function formatTime(date) {
    return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
  }

  function renderWeeklyTimeline(container, data) {
    const days = {};
    for (let i = 0; i < 7; i++) {
      const d = new Date(activeTimelineDate);
      const currentDay = activeTimelineDate.getDay();
      const distanceToMonday = currentDay === 0 ? -6 : 1 - currentDay;
      d.setDate(d.getDate() + distanceToMonday + i);
      const dateStr = formatDateIso(d);
      days[dateStr] = {
        date: d,
        sessions: []
      };
    }

    data.forEach(session => {
      const dateStr = session.StartedAt.substring(0, 10);
      if (days[dateStr]) {
        days[dateStr].sessions.push(session);
      }
    });

    Object.entries(days).forEach(([dateStr, dayInfo], idx) => {
      const totalSec = dayInfo.sessions.reduce((acc, s) => acc + s.Duration, 0);
      const dayLabel = dayInfo.date.toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' });
      const isToday = formatDateIso(dayInfo.date) === formatDateIso(new Date());

      const accordion = document.createElement('div');
      accordion.className = `week-day-accordion ${isToday ? 'open' : ''}`;

      accordion.innerHTML = `
        <div class=""week-day-header"" onclick=""toggleWeekDay(${idx}, this)"">
          <div class=""week-day-title-group"">
            <span class=""week-day-name"">${dayLabel}</span>
            <span class=""week-day-date"">(${dayInfo.sessions.length} sessions)</span>
          </div>
          <div class=""week-day-header-right"">
            <span class=""week-day-total"">${totalSec > 0 ? formatDuration(totalSec) : 'No activity'}</span>
            <svg class=""week-day-chevron"" viewBox=""0 0 24 24"" width=""18"" height=""18"" stroke=""currentColor"" stroke-width=""2"" fill=""none"" stroke-linecap=""round"" stroke-linejoin=""round""><polyline points=""6 9 12 15 18 9""></polyline></svg>
          </div>
        </div>
        <div class=""week-day-content ${isToday ? 'open' : ''}"" id=""week-day-content-${idx}"">
        </div>
      `;

      container.appendChild(accordion);

      const contentArea = accordion.querySelector(`#week-day-content-${idx}`);
      renderDailyTimeline(contentArea, dayInfo.sessions);
    });
  }

  function toggleWeekDay(idx, headerEl) {
    const accordion = headerEl.closest('.week-day-accordion');
    const content = document.getElementById(`week-day-content-${idx}`);
    if (content && accordion) {
      content.classList.toggle('open');
      accordion.classList.toggle('open');
    }
  }

  // Initialize Page
  window.addEventListener('DOMContentLoaded', () => {
    // Fill KPI Metrics
    document.getElementById('kpi-today').innerText = formatDuration(todayTotal);
    document.getElementById('kpi-week').innerText = formatDuration(weekTotal);
    document.getElementById('kpi-all').innerText = formatDuration(grandTotal);

    const aggToday = aggregateByCategory(rawToday);
    const aggWeek = aggregateByCategory(rawWeekApps);
    const aggAll = aggregateByCategory(rawAllTime);
    
    document.getElementById('kpi-today-sub').innerText = `${aggToday.length} categories`;
    document.getElementById('kpi-week-sub').innerText = `${aggWeek.length} categories`;
    document.getElementById('kpi-all-sub').innerText = `${aggAll.length} categories`;

    // Render Charts & Tables
    drawDonut('today-donut', 'today-legend', 'today-details', rawToday, todayTotal);
    drawDonut('week-donut', 'week-legend', 'week-details', rawWeekApps, weekTotal);
    drawWeekChart();
    drawAllTimeBreakdown();
    renderMappingsTable(processMappings);
    loadTimelineData();
  });
</script>
</body>
</html>";
    }
}

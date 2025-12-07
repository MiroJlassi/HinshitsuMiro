import lighthouse from "lighthouse";
import { launch } from "chrome-launcher";

export async function runLighthouse(url) {
  const chrome = await launch({ chromeFlags: ["--headless", "--no-sandbox"] });

  const options = {
    logLevel: "info",
    output: "json",
    onlyCategories: ["performance", "accessibility", "seo", "best-practices"],
    port: chrome.port
  };

  const runnerResult = await lighthouse(url, options);
  await chrome.kill();
  const lhr = runnerResult.lhr;
  const fullReport = {
    url: lhr.finalUrl,
    fetchTime: lhr.fetchTime,
    userAgent: lhr.userAgent,
    scores: {
      performance: lhr.categories.performance.score,
      accessibility: lhr.categories.accessibility.score,
      seo: lhr.categories.seo.score,
      bestPractices: lhr.categories["best-practices"].score
    },
    coreWebVitals: {
      lcp: lhr.audits["largest-contentful-paint"]?.numericValue ?? null,
      fid: lhr.audits["max-potential-fid"]?.numericValue ?? null,
      cls: lhr.audits["cumulative-layout-shift"]?.numericValue ?? null,
      tti: lhr.audits["interactive"]?.numericValue ?? null,
      fcp: lhr.audits["first-contentful-paint"]?.numericValue ?? null,
      tbt: lhr.audits["total-blocking-time"]?.numericValue ?? null
    },
    performanceDetails: {
      speedIndex: lhr.audits["speed-index"]?.numericValue ?? null,
      timeToInteractive: lhr.audits["interactive"]?.numericValue ?? null,
      firstMeaningfulPaint: lhr.audits["first-meaningful-paint"]?.numericValue ?? null,
      serverResponseTime: lhr.audits["server-response-time"]?.numericValue ?? null
    },
    accessibilityIssues: extractAuditItems(lhr, "accessibility"),
    seoIssues: extractAuditItems(lhr, "seo"),
    bestPracticesIssues: extractAuditItems(lhr, "best-practices"),
    opportunities: (lhr.categories.performance.auditRefs || [])
      .filter(a => a.group === "load-opportunities" || a.group === "perf-opportunities")
      .map(a => {
        const audit = lhr.audits[a.id];
        return {
          id: audit.id,
          title: audit.title,
          description: audit.description,
          savingsMs: audit.details?.overallSavingsMs ?? 0,
          savingsBytes: audit.details?.overallSavingsBytes ?? 0
        };
      }),
    diagnostics: {
      mainThreadWork: lhr.audits["mainthread-work-breakdown"]?.details?.items ?? [],
      bootupTime: lhr.audits["bootup-time"]?.details?.items ?? [],
      networkRequests: lhr.audits["network-requests"]?.details?.items ?? []
    },
    raw: {
      categories: Object.keys(lhr.categories || {}),
      audits: Object.keys(lhr.audits || {})
    }
  };

  return fullReport;
}

function extractAuditItems(lhr, category) {
  const cat = lhr.categories?.[category];
  if (!cat) return [];
  const items = [];

  (cat.auditRefs || []).forEach(ref => {
    const audit = lhr.audits[ref.id];
    if (!audit) return;
    if (audit.score !== null && audit.score !== 1) {
      items.push({
        id: audit.id,
        title: audit.title,
        description: audit.description,
        score: audit.score,
        details: audit.details?.items ?? null
      });
    }
  });

  return items;
}

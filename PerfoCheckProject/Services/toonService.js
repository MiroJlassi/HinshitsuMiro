export function convertToToon(lighthouse) {
  return {
    url: lighthouse.requestedUrl || lighthouse.url,
    fetchTime: lighthouse.fetchTime,
    scores: {
      performance: lighthouse.categories?.performance?.score || lighthouse.scores?.performance,
      accessibility: lighthouse.categories?.accessibility?.score || lighthouse.scores?.accessibility,
      seo: lighthouse.categories?.seo?.score || lighthouse.scores?.seo,
      bestPractices: lighthouse.categories?.['best-practices']?.score || lighthouse.scores?.bestPractices,
    },
    vitals: extractVitals(lighthouse),
    performance: {
      metrics: lighthouse.audits?.metrics?.details?.items?.[0] || lighthouse.performanceDetails,
      topOpportunities: processOpportunities(lighthouse)
    },
    accessibility: processAudits(lighthouse, 'accessibility', 10),
    seo: processAudits(lighthouse, 'seo', 5),
    bestPractices: processAudits(lighthouse, 'best-practices', 5),
    diagnostics: processDiagnostics(lighthouse)
  };
}

// Helpers
function extractVitals(lh) {
  if (lh.coreWebVitals) return lh.coreWebVitals; 

  const audits = lh.audits || {};
  return {
    lcp: audits['largest-contentful-paint']?.numericValue,
    cls: audits['cumulative-layout-shift']?.numericValue,
    fcp: audits['first-contentful-paint']?.numericValue,
    tbt: audits['total-blocking-time']?.numericValue
  };
}

function processOpportunities(lh) {
  const audits = lh.audits || {};
  const opps = Object.values(audits).filter(a => 
    a.details && 
    a.details.type === 'opportunity' && 
    (a.score !== null && a.score < 1)
  );

  return opps
    .sort((a, b) => (b.details?.overallSavingsMs || 0) - (a.details?.overallSavingsMs || 0))
    .slice(0, 5)
    .map(o => ({
      id: o.id,
      title: o.title,
      savingsMs: Math.round(o.details?.overallSavingsMs || 0),
      description: (o.description || "").split('[')[0].trim() 
    }));
}

function processAudits(lh, categoryId, limit) {
  const catRefs = lh.categories?.[categoryId]?.auditRefs || [];
  const issues = catRefs
    .map(ref => lh.audits?.[ref.id])
    .filter(audit => audit && audit.score !== null && audit.score < 1);

  const simplified = issues.map(audit => {
    let failureCount = 0;
    
    if (audit.details && audit.details.items) {
      failureCount = audit.details.items.length;
    }

    return {
      id: audit.id,
      title: audit.title,
      failureCount: failureCount > 0 ? failureCount : 1,
      description: (audit.description || "").split('[')[0].trim()
    };
  });

  return simplified
    .sort((a, b) => b.failureCount - a.failureCount)
    .slice(0, limit);
}

function processDiagnostics(lh) {
  const audits = lh.audits || {};
  const networkAudit = audits['network-requests'];
  
  if (!networkAudit || !networkAudit.details || !networkAudit.details.items) {
    return { summary: "No network details available" };
  }

  const requests = networkAudit.details.items;
  const totalBytes = requests.reduce((sum, req) => sum + (req.transferSize || 0), 0);
  
  const largest = [...requests]
    .sort((a, b) => (b.transferSize || 0) - (a.transferSize || 0))
    .slice(0, 5)
    .map(r => ({
      url: r.url.length > 50 ? '...' + r.url.slice(-40) : r.url,
      sizeKB: Math.round((r.transferSize || 0) / 1024),
      type: r.resourceType
    }));

  return {
    totalRequests: requests.length,
    totalSizeKB: Math.round(totalBytes / 1024),
    largestResources: largest,
    mainThreadWork: Math.round(audits['mainthread-work-breakdown']?.numericValue || 0)
  };
}
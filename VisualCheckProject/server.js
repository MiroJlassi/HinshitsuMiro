require('dotenv').config();
const fs = require('fs-extra');
const express = require('express');

const screenshotService = require('./services/screenshotService');
const aiAnalysisService = require('./services/aiAnalysisService');
const grammarService = require('./services/grammarService');
const siteReviewService = require('./services/siteReviewService');

const app = express();
app.use(express.json());

app.post('/analyze', async (req, res) => {
    const { url } = req.body;
    if (!url) return res.status(400).json({ error: 'Missing URL' });

    try {
        console.log(`Starting complete analysis for ${url}`);
        
        // Capture screenshots and extract links
        const { screenshots, links } = await screenshotService.capture(url);
        console.log('✓ Screenshots captured');
        console.log(`✓ Found ${links.length} links`);

        // Run all analyses in parallel
        const [uiAnalysis, grammarCheck, siteReviews] = await Promise.all([
            aiAnalysisService.analyzeUI(screenshots),
            grammarService.checkContent(screenshots[0].domContent),
            Promise.all(
                screenshots.map(shot => 
                    siteReviewService.reviewSite(shot.domJSON, shot.domContent, shot.viewport)
                )
            )
        ]);
        
        console.log('✓ UI/UX analysis complete');
        console.log('✓ Grammar check complete');
        console.log('✓ Site review complete');

        const siteReview = await siteReviewService.aggregateReviews(siteReviews);

        const report = {
            url,
            timestamp: new Date().toISOString(),
            links,
            uiAnalysis,
            grammarCheck,
            siteReview
        };

        await fs.writeJson('report.json', report, { spaces: 2 });
        console.log('✓ Analysis complete! Report saved.');

        await screenshotService.cleanup();

        res.json(report);
    } catch (err) {
        console.error(err);
        await screenshotService.cleanup().catch(() => {});
        res.status(500).json({ error: 'Analysis failed', details: err.message });
    }
});

const PORT = process.env.PORT || 4000;
app.listen(PORT, () => console.log(`UI/UX Analyzer running on port ${PORT}`));

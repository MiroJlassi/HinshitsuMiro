const { AzureOpenAI } = require('openai');

class SiteReviewService {
    constructor() {
        this.openai = new AzureOpenAI({
            endpoint: process.env.AZURE_OPENAI_ENDPOINT,
            apiKey: process.env.AZURE_OPENAI_KEY,
            apiVersion: process.env.AZURE_OPENAI_API_VERSION,
            deployment: process.env.AZURE_GPT_DEPLOYMENT
        });
    }

    async reviewSite(domJSON, domContent, viewport) {
        const prompt = `
Analyze this website for overall quality and responsiveness for ${viewport} viewport:

DOM Structure Analysis:
- Semantic HTML usage
- Accessibility features (ARIA labels, alt texts)
- Mobile-friendliness
- Navigation structure
- Content organization
- Performance indicators (DOM complexity)

Return a JSON object with:
{
  "scores": {
    "accessibility": 0-10,
    "semanticHTML": 0-10,
    "navigation": 0-10,
    "responsiveness": 0-10,
    "overall": 0-10
  },
  "issues": ["up to 3 most critical structural/technical issues only"],
  "recommendations": ["up to 5 most impactful improvements for better UX only"]
}

IMPORTANT: Limit to exactly 3 issues and 5 recommendations maximum.
`;

        const response = await this.openai.chat.completions.create({
            model: process.env.AZURE_GPT_DEPLOYMENT,
            messages: [
                { role: 'system', content: 'You are a web development and UX expert.' },
                {
                    role: 'user',
                    content: `${prompt}\n\nDOM Summary: ${JSON.stringify(domJSON).substring(0, 3000)}`
                }
            ]
        });

        const output = response.choices[0].message.content;
        return JSON.parse(output);
    }

    async aggregateReviews(reviews) {
        const aggregate = {
            viewportReviews: reviews,
            overallScore: 0,
            criticalIssues: [],
            recommendations: []
        };

        // Calculate average scores
        let totalScore = 0;
        reviews.forEach(review => {
            totalScore += review.scores.overall;
            aggregate.criticalIssues.push(...review.issues);
            aggregate.recommendations.push(...review.recommendations);
        });

        aggregate.overallScore = (totalScore / reviews.length).toFixed(1);
        
        aggregate.criticalIssues = [...new Set(aggregate.criticalIssues)].slice(0, 3);
        aggregate.recommendations = [...new Set(aggregate.recommendations)].slice(0, 5);

        const score = parseFloat(aggregate.overallScore);
        if (score >= 8) {
            aggregate.encouragement = "Outstanding work! Your website demonstrates excellent quality across all areas. Keep up the fantastic work! 🎉";
        } else if (score >= 6) {
            aggregate.encouragement = "Great job! Your website has a solid foundation. With a few improvements, it'll be even better! 💪";
        } else if (score >= 4) {
            aggregate.encouragement = "Good start! You're on the right track. Implementing the recommendations will significantly enhance your site. Keep going! 🚀";
        } else {
            aggregate.encouragement = "Every great website starts somewhere! Focus on the key recommendations, and you'll see amazing improvements. You've got this! 💡";
        }

        return aggregate;
    }
}

module.exports = new SiteReviewService();

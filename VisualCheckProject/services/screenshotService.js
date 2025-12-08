const playwright = require('playwright');
const fs = require('fs-extra');
const path = require('path');

class ScreenshotService {
    constructor() {
        this.sizes = [
            { name: 'desktop', width: 1920, height: 1080 },
            { name: 'tablet', width: 768, height: 1024 },
            { name: 'mobile', width: 375, height: 667 }
        ];
    }

    async capture(url) {
        // miro we have to check for available/already installed chrome instance
        const browser = await playwright.chromium.launch({ 
            headless: true,
            executablePath: '/usr/bin/chromium'
        });
        const page = await browser.newPage();
        
        try {
            await page.goto(url, { 
                waitUntil: 'networkidle', 
                timeout: 60000 
            });
        } catch (err) {
            // Fallback to 'load' if 'networkidle' times out
            console.log('Network idle timeout, falling back to load event...');
            await page.goto(url, { 
                waitUntil: 'load', 
                timeout: 30000 
            });
        }
        
        await page.waitForTimeout(2000);

        await fs.ensureDir('screenshots');

        const links = await this.extractLinks(page);

        const results = [];

        for (const size of this.sizes) {
            await page.setViewportSize({ width: size.width, height: size.height });

            const screenshotPath = path.join('screenshots', `${size.name}.png`);
            await page.screenshot({ 
                path: screenshotPath, 
                fullPage: true,
                timeout: 60000
            });

            const domContent = await page.content();
            const domJSON = await this.extractDOMStructure(page);

            results.push({
                viewport: size.name,
                screenshotPath,
                domContent,
                domJSON
            });
        }

        await browser.close();
        return { screenshots: results, links };
    }

    async extractDOMStructure(page) {
        return await page.evaluate(() => {
            function nodeToObject(node) {
                if (!node) return null;
                const obj = {
                    tag: node.tagName,
                    children: [],
                    text: node.innerText || '',
                    id: node.id || '',
                    classList: node.className ? node.className.split(' ') : [],
                    styles: node.style ? node.style.cssText : ''
                };
                if (node.children) {
                    for (const child of node.children) {
                        obj.children.push(nodeToObject(child));
                    }
                }
                return obj;
            }
            return nodeToObject(document.body);
        });
    }

    async extractLinks(page) {
        return await page.evaluate(() => {
            const links = Array.from(document.querySelectorAll('a[href]'));
            return links.map(link => ({
                text: link.innerText.trim() || link.getAttribute('aria-label') || 'No text',
                href: link.href,
                title: link.title || ''
            })).filter(link => link.href && !link.href.startsWith('javascript:'));
        });
    }

    async cleanup() {
        try {
            await fs.remove('screenshots');
            console.log('✓ Screenshots cleaned up');
        } catch (err) {
            console.error('Failed to cleanup screenshots:', err.message);
        }
    }
}

module.exports = new ScreenshotService();

// Component testing script for Puppeteer
const components = [
  { name: 'Form', url: 'http://localhost:5125/components/form', category: 'FORM' },
  { name: 'FormField', url: 'http://localhost:5125/components/formfield', category: 'FORM' },
  { name: 'Avatar', url: 'http://localhost:5125/components/avatar', category: 'DATADISPLAY' },
  { name: 'Badge', url: 'http://localhost:5125/components/badge', category: 'DATADISPLAY' },
  { name: 'Chart', url: 'http://localhost:5125/components/chart', category: 'DATADISPLAY' },
  { name: 'DataGrid', url: 'http://localhost:5125/components/datagrid', category: 'DATADISPLAY' },
  { name: 'Divider', url: 'http://localhost:5125/components/divider', category: 'DATADISPLAY' },
  { name: 'Separator', url: 'http://localhost:5125/components/separator', category: 'DATADISPLAY' },
  { name: 'Skeleton', url: 'http://localhost:5125/components/skeleton', category: 'DATADISPLAY' },
  { name: 'Table', url: 'http://localhost:5125/components/table', category: 'DATADISPLAY' },
  { name: 'Tag', url: 'http://localhost:5125/components/tag', category: 'DATADISPLAY' },
  { name: 'TreeViewer', url: 'http://localhost:5125/components/treeviewer', category: 'DATADISPLAY' },
  { name: 'ValidatedInput', url: 'http://localhost:5125/components/validatedinput', category: 'DATADISPLAY' }
];

async function testComponent(page, component) {
  const result = {
    name: component.name,
    category: component.category,
    url: component.url,
    status: '✅',
    issues: []
  };

  try {
    await page.goto(component.url, { waitUntil: 'networkidle2', timeout: 10000 });

    const pageData = await page.evaluate(() => {
      const title = document.querySelector('h1')?.textContent?.trim();
      const hasPreview = !!document.querySelector('.preview, [class*="preview"]');
      const hasError = document.body.textContent.includes('unhandled error') ||
                       document.body.textContent.includes('An error has occurred');
      const previewMessage = document.querySelector('.preview')?.textContent || '';
      const hasMissingPreview = previewMessage.includes('preview will be shown') ||
                                previewMessage.includes('will be available');

      return {
        title,
        hasPreview,
        hasError,
        hasMissingPreview,
        inputCount: document.querySelectorAll('input, textarea, select').length,
        buttonCount: document.querySelectorAll('button').length
      };
    });

    // Determine status
    if (pageData.hasError) {
      result.status = '🔴';
      result.issues.push('Page has unhandled error');
    } else if (pageData.hasMissingPreview) {
      result.status = '⚠️';
      result.issues.push('Preview not implemented');
    } else if (!pageData.hasPreview) {
      result.status = '⚠️';
      result.issues.push('No preview section found');
    }

    result.pageData = pageData;

  } catch (error) {
    result.status = '🔴';
    result.issues.push(`Navigation error: ${error.message}`);
  }

  return result;
}

module.exports = { components, testComponent };

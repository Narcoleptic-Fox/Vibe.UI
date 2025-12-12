/**
 * Vibe.UI Resizable Component JS Module
 * Handles drag operations for resizable panels
 */

export function createDragHandler(dotNetRef, isHorizontal) {
    let isDragging = false;

    const handleMouseMove = (e) => {
        if (!isDragging) return;
        e.preventDefault();
        dotNetRef.invokeMethodAsync('HandleDragMove', e.clientX, e.clientY);
    };

    const handleMouseUp = () => {
        if (!isDragging) return;
        isDragging = false;
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        dotNetRef.invokeMethodAsync('HandleDragEnd');
    };

    return {
        startDrag: function() {
            isDragging = true;
            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);
            document.body.style.cursor = isHorizontal ? 'col-resize' : 'row-resize';
            document.body.style.userSelect = 'none';
        },
        dispose: function() {
            isDragging = false;
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        }
    };
}

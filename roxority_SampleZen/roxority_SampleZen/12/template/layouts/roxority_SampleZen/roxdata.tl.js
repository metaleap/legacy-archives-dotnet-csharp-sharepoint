function roxCheckForChanges() {
	var $b;
	jQuery('#roxtoolsaveundo').css('visibility', ((jQuery('#roxds_farm').text() != roxdsFarm) || (jQuery('#roxds_site').text() != roxdsSite)) ? 'visible' : 'hidden');
	if (roxdsEditMode && (($b = jQuery(document.body)).scrollTop() || $b.scrollLeft()))
		$b.scrollTop(0).scrollLeft(0);
}
function roxCloseDataSourceEditor() {
	if (roxdsEditMode)
		jQuery('#roxdataeditor').fadeOut(function() {
			jQuery(document.body).attr('scroll', 'yes');
			roxdsEditMode = false;
		});
}
function roxOpenDataSourceEditor() {
	if (!roxdsEditMode) {
		roxdsEditMode = true;
		jQuery(document.body).scrollTop(0).scrollLeft(0).attr('scroll', 'no');
		jQuery('#roxdataeditor').fadeIn(roxClearCssFilter);
	}
}
function roxToggleDataTab(elem) {
	if (roxdsTab != (elem.id + '_box')) {
		jQuery('#' + roxdsTab).slideUp(function() {
			jQuery('#' + (roxdsTab = elem.id + '_box')).slideDown();
		});
		jQuery('#roxdataeditor .ms-templatepickerselected').removeClass('ms-templatepickerselected').addClass('ms-templatepickerunselected');
		jQuery('#' + elem.id).removeClass('ms-templatepickerunselected').addClass('ms-templatepickerselected');
	}
}

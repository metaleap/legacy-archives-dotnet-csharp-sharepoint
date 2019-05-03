var prodName = '', prodNameLower = '', inner = '', roxCfgPageTitle, picHeights = {}, thumbSize = '96px';
jQuery(document).ready(function() {
	var $pics = jQuery('img.rox-docpic');
	if (location.href.indexOf('/default.aspx') >= 0)
		jQuery('span#roxconfiglink').html('<a href="?cfg=cfg">' + roxCfgPageTitle + '</a>');
	$pics.each(function(index, elem) {
		var $elem = jQuery(elem);
		$elem.css('rheight', $elem.css('height')).css('height', thumbSize);
	});
	$pics.click(function() {
		var $elem = jQuery(this);
		if($elem.css('height') == thumbSize)
			$elem.animate({ 'height': $elem.css('rheight') }, 'fast');
		else
			$elem.animate({ 'height': thumbSize }, 'slow');
	});
	jQuery('#roxDocToc').html(inner);
	$pics = jQuery('#licdiv li');
	if ($pics.length > 1) {
		$pics[0].style.backgroundImage = "url('/_layouts/images/checkitems.gif')";
		$pics[1].style.backgroundImage = "url('/_layouts/images/gtaclaim.gif')";
	}
});
function roxGoLicense() {
	window.open(roxBaseUrl + prodNameLower + '-license/');
}
function roxGoReset(name, scope) {
	var elem = document.getElementById('cfg_' + scope + '_' + name);
	document.getElementById('chk_' + scope + '_' + name).disabled = elem.disabled = true;
	location.replace('?cfg=reset&name=' + name + '&scope=' + scope);
}
function roxGoSave(name, scope) {
	var elem = document.getElementById('cfg_' + scope + '_' + name), value = ((elem.tagName == 'SELECT') ? elem.options[elem.selectedIndex].value : ((jQuery(elem).attr('type').toLowerCase() == 'checkbox') ? (elem.checked ? 'true' : 'false') : jQuery(elem).val()));
	document.getElementById('chk_' + scope + '_' + name).disabled = elem.disabled = true;
	location.replace('?cfg=save&name=' + name + '&scope=' + scope + '&value=' + escape(value));
}
function roxGoSite(url) {
	var pos = location.href.indexOf('#'), pos2 = location.href.indexOf('?');
	location.href = (((pos < 0) ? location.href : location.href.substr(0, pos)) + ((pos2 < 0) ? '?' : '&') + "roxgosite=" + escape(url));
}
function roxGoWeb(id) {
	var pos = location.href.indexOf('#'), pos2 = location.href.indexOf('?');
	location.href = (((pos < 0) ? location.href : location.href.substr(0, pos)) + ((pos2 < 0) ? '?' : '&') + "roxgoweb=" + escape(id));
}

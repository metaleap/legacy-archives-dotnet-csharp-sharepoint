var rpzMst = null, roxPrintPage = '', roxDlgOnLoad = false, roxPageLoaded = false;

function roxDoPrint() {
	window.self.focus();
	window.self.print();
}
function roxPostReady(load) {
	var tmp;
	if (roxPrintPage && roxPrintPage.length) {
		if (roxPrintPage != '1')
			jQuery('.ms-bodyareacell').prepend(roxPrintPage);
		/*if ((tmp = jQuery('div.ms-bodyareacell')).length)
			tmp.prependTo('form#aspnetForm');
		else */if ((tmp = jQuery('td.ms-bodyareacell')).length)
			jQuery('form#aspnetForm').html(tmp.html());
	}
	if (roxPageLoaded && roxDlgOnLoad)
		setTimeout(roxDoPrint, 100);
}

function roxPrintZoom(val) {
	jQuery('form#aspnetForm').css('zoom', val + "%");
}

try {
	rpzMst = MoveSiteTitle;
} catch(e) {
}
if (!rpzMst)
	MoveSiteTitle = function() {};

jQuery(document).ready(function() {
	var sel = document.getElementById('roxpzzoomer');
	if (sel) {
		sel.selectedIndex = 1;
		roxPrintZoom('120');
	}
	roxPostReady(false);
});

jQuery(window).load(function() {
	roxPageLoaded = true;
});

/**
 * @license Copyright (c) 2003-2017, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.md or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function( config ) {
	// Define changes to default configuration here. For example:
	// config.language = 'fr';
	// config.uiColor = '#AADC6E';
    config.removePlugins = 'save, NewPage, DocProps,Preview,Print,Templates,document,Form,Checkbox,Radio,TextField,Textarea,Select,Button,image,HiddenField,Iframe,About, flash';
    config.removeButtons = 'NewPage, DocProps,Preview,Print,Templates,document,Form,Checkbox,Radio,TextField,Textarea,Select,Button,image,HiddenField,Iframe,About';
    config.startupFocus = true;
};

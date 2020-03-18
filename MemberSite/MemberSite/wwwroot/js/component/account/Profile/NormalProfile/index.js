import {CustInfo} from './CustInfo';

(function ($, React, ReactDOM, i18n, axios, moment) {

    const Panel = ()=>{
        return (
            <>
                <div className="heading" role="desktop-only">
                    <span className="text">{i18n.lbl_Profile}</span>
                </div>
                <CustInfo />
            </>
        )
    }

    ReactDOM.render(<Panel />, document.getElementById('Profile'));
})(jQuery, React, ReactDOM, i18n, axios, moment);
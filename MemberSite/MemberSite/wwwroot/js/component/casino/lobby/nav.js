import { Page } from "./dataDefs";
import { DropdownBox } from "./components/dropDownBox";
import { Provider } from "../../../../datas/Provider";

export const Nav = (props) => {
    let desktopPages = [];
    let mobilePages = [];
    let propmoDisplayText = window.langCode === 'cs' || window.langCode === 'ch' ? '熱!' : 'HOT!';
    Object.keys(Page).forEach((key) => {
        let pageInfo = Page[key];
        let active = props.SelectedPage.displayText === pageInfo.displayText;
        desktopPages.push(
            <a
                key={`d-p-${key}`}
                onClick={props.HandleFilter.bind(null, pageInfo, "Page")}
                className={active ? "active" : ""}
            >
                <span>{pageInfo.displayText}</span>      
                {
                    pageInfo.hot === true ? <div className="hot">{propmoDisplayText}</div> : null
                }
            </a>
        );

        mobilePages.push(pageInfo);
    });

    const InputKeyword = (e) => {
        props.HandleFilter(e.target.value, "Keyword");
    };

    const OnKeyPress = (e) => {
        if (e.key === "Enter") {
            props.SearchGame();
        }
    };

    return (
        <div className="CasinoSelect_nav" role="desktop-only">
            {desktopPages}
            <DropdownBox
                OnSelectItem={props.HandleFilter}
                DataColumn="Page"
                Options={mobilePages}
                SelectedItem={props.SelectedPage.displayText}
                SubClass="mobile-sort-select"
            />
            <div className="textfield">
                <input
                    type="text"
                    className="textfield__input"
                    defaultValue=""
                    onChange={InputKeyword}
                    onKeyPress={OnKeyPress}
                />
                <span
                    className="textfield__bg"
                    style={{ background: "#fff" }}
                ></span>
                <a
                    className="btn btn-icon"
                    data-visibility="true"
                    onClick={props.SearchGame}
                >
                    <i className="icon icon-search"></i>
                </a>
            </div>
            <DropdownBox
                OnSelectItem={props.HandleFilter}
                DataColumn="Provider"
                Options={[
                    { value: "All", name: "All", displayText: i18n.lbl_All },
                    ...Provider.filter((p) => {
                        return TpIds === null
                            ? isLogin === "1"
                                ? false
                                : true
                            : TpIds.includes(p.value);
                    }),
                ]}
                SelectedItem={props.SelectedProvider.displayText}
                HasAll={true}
            />
        </div>
    );
};

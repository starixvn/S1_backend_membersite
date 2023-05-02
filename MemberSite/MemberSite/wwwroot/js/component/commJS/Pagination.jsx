export class Pagination extends React.Component {
    constructor(props) {
        super(props);
    }

    onPaging(value) {
        this.props.onPaging(value);
    }

    render() {
        let pageNumber = this.props.PageNumber||0;
        let totalPages = this.props.TotalPages||0;
        return (
            totalPages == 0 ?
            null :
            <div className="pagination">
                <a className="btn btn-icon text-primary" data-disabled={pageNumber==1} onClick={pageNumber==1 ? null: this.onPaging.bind(this, 1)}>
                    <i className="icon icon-arrow-left-end"></i>
                </a>
                <a className="btn btn-icon text-primary" data-disabled={pageNumber==1} onClick={pageNumber==1 ? null: this.onPaging.bind(this, pageNumber-1)}>
                    <i className="icon icon-arrow-left"></i>
                </a>
                <div className="pagination__pages">
                    <span className="text">{i18n.lbl_Page} {pageNumber} {i18n.lbl_Of} {totalPages}</span>
                </div>
                <a className="btn btn-icon text-primary" data-disabled={pageNumber==totalPages} onClick={pageNumber==totalPages ? null: this.onPaging.bind(this, pageNumber+1)}>
                    <i className="icon icon-arrow-right"></i>
                </a>
                <a className="btn btn-icon text-primary" data-disabled={pageNumber==totalPages} onClick={pageNumber==totalPages ? null: this.onPaging.bind(this, totalPages)}>
                    <i className="icon icon-arrow-right-end"></i>
                </a>
            </div>
        )
    }
}
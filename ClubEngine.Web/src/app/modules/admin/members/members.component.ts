import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MembersService } from './members.service';
import { MemberDisplayItem, MemberStats } from 'app/api/api';
import { Observable } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { MembersHistoryComponent } from "../members-history/members-history.component";
import { CdkScrollable } from '@angular/cdk/scrolling';
import { MatChipsModule } from '@angular/material/chips';
import { MembersHistoryService } from '../members-history/members-history.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MemberComponent } from "../member/member.component";

@Component({
  selector: 'app-members',
  imports: [TranslatePipe, AsyncPipe, MatIconModule, MatInputModule, RouterModule, MemberComponent, MembersHistoryComponent, MembersHistoryComponent, CdkScrollable, MatChipsModule, MatFormFieldModule, MemberComponent],
  templateUrl: './members.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MembersComponent {
  filterByTypeIds: string[];
  searchString: string;
  constructor(private membersService: MembersService,
    private statsService: MembersHistoryService) { }

  get members$(): Observable<MemberDisplayItem[]> {
    return this.membersService.members$;
  }
  get stats$(): Observable<MemberStats> {
    return this.statsService.stats$;
  }

  onSelectionChange(selectedMembershipTypeId: string[]) {
    this.filterByTypeIds = selectedMembershipTypeId;
    this.refresh();
  }

  filterByQuery(searchString: string) {
    this.searchString = searchString;
    this.refresh();
  }

  getColor(color: string) {
    return `background-color:${color};`;
  }

  refresh() {
    this.membersService.fetch(this.filterByTypeIds, this.searchString).subscribe();
  }
}
